using System.Text.Json;
using Tamin.Integration.Mapping;
using PilotModels = Tamin.Client.Soa.Pilot.Models;
using ProdModels = Tamin.Client.Soa.Prod.Models;

namespace Tamin.Integration.Tests;

public sealed class PrescriptionRequestMapperTests
{
    // Source contract fingerprint: ffc1f769ae98006a785ecfa77613e7ef7a892d5106a35a9b4b31154b0a16a6d8
    private readonly PrescriptionRequestMapper mapper = new();

    [Theory]
    [InlineData("\"0\"", DentalServiceFlag.Zero)]
    [InlineData("\"1\"", DentalServiceFlag.One)]
    [InlineData("0", DentalServiceFlag.Zero)]
    [InlineData("1", DentalServiceFlag.One)]
    public void D07_normalizes_documented_string_and_numeric_values(string json, DentalServiceFlag expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<DentalServiceFlag>(json));
        var input = Input(details: [Detail(expected)]);
        Assert.Equal(expected == DentalServiceFlag.Zero ? ProdModels.NoteDetailEprsc_isDentalService.Zero : ProdModels.NoteDetailEprsc_isDentalService.One,
            mapper.MapProduction(input).NoteDetailEprscs![0].IsDentalService);
        Assert.Equal(expected == DentalServiceFlag.Zero ? PilotModels.NoteDetailEprsc_isDentalService.Zero : PilotModels.NoteDetailEprsc_isDentalService.One,
            mapper.MapPilot(input).NoteDetailEprscs![0].IsDentalService);
        Assert.Equal($"\"{(expected == DentalServiceFlag.Zero ? "0" : "1")}\"", JsonSerializer.Serialize(expected));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("\"2\"")]
    [InlineData("\"dental\"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("{}")]
    public void D07_rejects_other_wire_values_with_typed_validation(string json)
    {
        var failure = Assert.Throws<TaminRequestValidationException>(() => JsonSerializer.Deserialize<DentalServiceFlag>(json));
        Assert.Equal("isDentalService", failure.FieldName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void D06_requires_nonblank_mobile_without_inventing_a_format(string mobile)
    {
        var failure = Assert.Throws<TaminRequestValidationException>(() => mapper.MapProduction(Input(mobile)));
        Assert.Equal("mobile", failure.FieldName);
    }

    [Fact]
    public void D05_and_D09_through_D11_map_independent_canonical_members_for_both_environments()
    {
        var referral = new PrescriptionReferralInput(
            [new("complaint-ids", "complaint-id")], new("special"), [new("X46.49")], 17, "message", 18,
            "patient", 2, 3, [new(new(4), "feedback", 5)], "14040610", "siam");
        var input = Input(details: [Detail(DentalServiceFlag.One)], referrals: [referral]);

        var prod = mapper.MapProduction(input);
        var pilot = mapper.MapPilot(input);
        Assert.Single(prod.NoteDetailEprscs!); Assert.Single(prod.NoteDetailsReferralList!);
        Assert.Single(pilot.NoteDetailEprscs!); Assert.Single(pilot.NoteDetailsReferralList!);
        var detail = prod.NoteDetailEprscs![0];
        var mappedReferral = prod.NoteDetailsReferralList![0];
        Assert.Equal(71, detail.NoteDetailDrug!.IcdId);
        Assert.Equal("X46.49", mappedReferral.Icd10s![0].IcdId);
        Assert.Equal(17, mappedReferral.Id);
        Assert.Equal("14040610", mappedReferral.ReferralHijriDate);
        Assert.Equal("complaint-ids", mappedReferral.Complaints![0].ComplaintIDs);
        Assert.Equal("complaint-id", mappedReferral.Complaints![0].Id);
        var pilotDetail = pilot.NoteDetailEprscs![0];
        var pilotReferral = pilot.NoteDetailsReferralList![0];
        Assert.Equal(71, pilotDetail.NoteDetailDrug!.IcdId);
        Assert.Equal("X46.49", pilotReferral.Icd10s![0].IcdId);
        Assert.Equal(17, pilotReferral.Id);
        Assert.Equal("14040610", pilotReferral.ReferralHijriDate);
        Assert.Equal("complaint-ids", pilotReferral.Complaints![0].ComplaintIDs);
        Assert.Equal("complaint-id", pilotReferral.Complaints![0].Id);
        Assert.Equal(input.DocNationalCode, pilot.ClientId);
    }

    [Fact]
    public void Mobile_is_optional_in_guide_v1_10_1_so_omission_is_allowed_but_blank_is_not()
    {
        var prod = mapper.MapProduction(Input(mobile: null));
        var pilot = mapper.MapPilot(Input(mobile: null));
        Assert.Null(prod.Mobile); Assert.Null(pilot.Mobile);
        Assert.DoesNotContain("\"mobile\"", Serialize(prod));
        Assert.DoesNotContain("\"mobile\"", Serialize(pilot));
        Assert.Equal("0912", mapper.MapProduction(Input()).Mobile);
    }

    [Fact]
    public void V1_10_1_request_fields_are_mapped_for_both_environments()
    {
        var referral = new PrescriptionReferralInput(null, new("special", "MAXA"), null, null, null, null, null, null, null, null, null, null);
        var input = Input(details: [Detail(DentalServiceFlag.One)], referrals: [referral]);

        var prod = mapper.MapProduction(input);
        Assert.Equal(("siam-id", "SNOMEDCT"), (prod.SiamId, prod.Terminology));
        Assert.Equal("complaints", prod.NoteHeadEprscSupplementary!.ComplaintIDs);
        var prodDetail = prod.NoteDetailEprscs![0];
        Assert.Equal(("dose", "dose-code", 307166007, 71), (prodDetail.Dose, prodDetail.DoseCode, prodDetail.MethodUsage!.Id, prodDetail.NoteDetailDrug!.IcdId));
        Assert.Equal(("special", "MAXA"), (prod.NoteDetailsReferralList![0].DocSpecReferred!.SpecCode, prod.NoteDetailsReferralList![0].DocSpecReferred!.Terminology));

        var pilot = mapper.MapPilot(input);
        Assert.Equal(("siam-id", "SNOMEDCT"), (pilot.SiamId, pilot.Terminology));
        Assert.Equal("complaints", pilot.NoteHeadEprscSupplementary!.ComplaintIDs);
        var pilotDetail = pilot.NoteDetailEprscs![0];
        Assert.Equal(("dose", "dose-code", 307166007, 71), (pilotDetail.Dose, pilotDetail.DoseCode, pilotDetail.MethodUsage!.Id, pilotDetail.NoteDetailDrug!.IcdId));
        Assert.Equal(("special", "MAXA"), (pilot.NoteDetailsReferralList![0].DocSpecReferred!.SpecCode, pilot.NoteDetailsReferralList![0].DocSpecReferred!.Terminology));
    }

    [Fact]
    public void V1_10_1_request_fields_use_the_documented_wire_names_and_drop_the_removed_ones()
    {
        var input = Input(details: [Detail(DentalServiceFlag.One)]);
        foreach (var json in new[] { Serialize(mapper.MapProduction(input)), Serialize(mapper.MapPilot(input)) })
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            Assert.Equal("siam-id", root.GetProperty("siamId").GetString());
            Assert.Equal("SNOMEDCT", root.GetProperty("terminology").GetString());
            Assert.Equal("complaints", root.GetProperty("noteHeadEprscSupplementary").GetProperty("complaintIDs").GetString());
            Assert.False(root.TryGetProperty("Complaint", out _) || root.TryGetProperty("complaint", out _));
            var detail = root.GetProperty("noteDetailEprscs")[0];
            Assert.Equal("dose-code", detail.GetProperty("doseCode").GetString());
            Assert.Equal(307166007, detail.GetProperty("methodUsage").GetProperty("id").GetInt32());
            Assert.Equal(71, detail.GetProperty("noteDetailDrug").GetProperty("icdId").GetInt32());
            Assert.False(detail.TryGetProperty("DiagnosisID", out _) || detail.TryGetProperty("diagnosisID", out _));
        }
    }

    [Fact]
    public void D08_maps_every_dentistry_grid_item_as_a_collection()
    {
        var input = new DentistRuleInput([new(new("A", "T"), "11"), new(new("B", null), "12")], "doctor", "patient", null, null);
        var prod = mapper.MapProduction(input).AllGridData!;
        var pilot = mapper.MapPilot(input).AllGridData!;
        Assert.Equal(2, prod.Count); Assert.Equal(2, pilot.Count);
        var prodFirstService = Assert.IsType<ProdModels.Service>(prod[0].SrvId);
        var prodSecondService = Assert.IsType<ProdModels.Service>(prod[1].SrvId);
        var pilotFirstService = Assert.IsType<PilotModels.Service>(pilot[0].SrvId);
        var pilotSecondService = Assert.IsType<PilotModels.Service>(pilot[1].SrvId);
        Assert.Equal(("A", "T", "11"), (prodFirstService.SrvCode, prodFirstService.Terminology, prod[0].ToothId));
        Assert.Equal(("B", null, "12"), (prodSecondService.SrvCode, prodSecondService.Terminology, prod[1].ToothId));
        Assert.Equal(("A", "T", "11"), (pilotFirstService.SrvCode, pilotFirstService.Terminology, pilot[0].ToothId));
        Assert.Equal(("B", null, "12"), (pilotSecondService.SrvCode, pilotSecondService.Terminology, pilot[1].ToothId));
    }

    [Theory]
    [InlineData("01", ProdModels.ServiceType_srvType.ZeroOne, PilotModels.ServiceType_srvType.ZeroOne)]
    [InlineData("16", ProdModels.ServiceType_srvType.OneSix, PilotModels.ServiceType_srvType.OneSix)]
    [InlineData("17", ProdModels.ServiceType_srvType.OneSeven, PilotModels.ServiceType_srvType.OneSeven)]
    public void Service_type_is_sent_for_every_item_in_both_environments(string code, ProdModels.ServiceType_srvType production, PilotModels.ServiceType_srvType pilot)
    {
        var detail = Detail(DentalServiceFlag.Zero) with { SrvId = new("code", null, code) };
        var input = Input(details: [detail]);
        Assert.Equal(production, mapper.MapProduction(input).NoteDetailEprscs![0].SrvId!.SrvType!.SrvType);
        Assert.Equal(pilot, mapper.MapPilot(input).NoteDetailEprscs![0].SrvId!.SrvType!.SrvType);
        Assert.Contains($"\"srvType\":{{\"srvType\":\"{code}\"}}", Serialize(mapper.MapProduction(input).NoteDetailEprscs![0].SrvId!));
    }

    [Fact]
    public void Service_type_is_omitted_when_not_given_and_rejected_when_undocumented()
    {
        Assert.Null(mapper.MapProduction(Input(details: [Detail(DentalServiceFlag.Zero)])).NoteDetailEprscs![0].SrvId!.SrvType);
        var invalid = Detail(DentalServiceFlag.Zero) with { SrvId = new("code", null, "99") };
        var failure = Assert.Throws<TaminRequestValidationException>(() => mapper.MapProduction(Input(details: [invalid])));
        Assert.Equal("srvType", failure.FieldName);
    }

    private static PrescriptionCreateInput Input(string? mobile = "0912", IReadOnlyList<PrescriptionDetailInput>? details = null, IReadOnlyList<PrescriptionReferralInput>? referrals = null,
        string? siamId = "siam-id", string? terminology = "SNOMEDCT") =>
        new("comments", "creator", "doc", "doc-mobile", "national", "14040611", mobile,
            details, referrals, new("complaints"), "patient", "14040610", new(2), 42, siamId, terminology);

    private static PrescriptionDetailInput Detail(DentalServiceFlag flag) =>
        new("14040610", "dose", "dose-code", new(3), "illness", flag, new(307166007), new(71),
            9, "plan", 1, "repeat", new("service", "terminology"), 2, new(4), "11");

    private static string Serialize(Microsoft.Kiota.Abstractions.Serialization.IParsable value)
    {
        using var writer = new Microsoft.Kiota.Serialization.Json.JsonSerializationWriter();
        writer.WriteObjectValue(null, value);
        using var reader = new StreamReader(writer.GetSerializedContent());
        return reader.ReadToEnd();
    }
}
