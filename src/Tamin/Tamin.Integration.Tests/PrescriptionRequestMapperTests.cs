using System.Text.Json;
using Tamin.Integration.Mapping;
using PilotModels = Tamin.Client.Soa.Pilot.Models;
using ProdModels = Tamin.Client.Soa.Prod.Models;

namespace Tamin.Integration.Tests;

public sealed class PrescriptionRequestMapperTests
{
    // Source contract fingerprint: 40fb25238c71df67c78854e4424a03a8aee9b809d2e32595b484a0de0071a08a
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
        Assert.Equal(71, detail.DiagnosisID!.IcdId);
        Assert.Equal("X46.49", mappedReferral.Icd10s![0].IcdId);
        Assert.Equal(17, mappedReferral.Id);
        Assert.Equal("14040610", mappedReferral.ReferralHijriDate);
        Assert.Equal("complaint-ids", mappedReferral.Complaints![0].ComplaintIDs);
        Assert.Equal("complaint-id", mappedReferral.Complaints![0].Id);
        var pilotDetail = pilot.NoteDetailEprscs![0];
        var pilotReferral = pilot.NoteDetailsReferralList![0];
        Assert.Equal(71, pilotDetail.DiagnosisID!.IcdId);
        Assert.Equal("X46.49", pilotReferral.Icd10s![0].IcdId);
        Assert.Equal(17, pilotReferral.Id);
        Assert.Equal("14040610", pilotReferral.ReferralHijriDate);
        Assert.Equal("complaint-ids", pilotReferral.Complaints![0].ComplaintIDs);
        Assert.Equal("complaint-id", pilotReferral.Complaints![0].Id);
        Assert.Equal(input.DocNationalCode, pilot.ClientId);
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

    private static PrescriptionCreateInput Input(string mobile = "0912", IReadOnlyList<PrescriptionDetailInput>? details = null, IReadOnlyList<PrescriptionReferralInput>? referrals = null) =>
        new("comments", new("complaints"), "creator", "doc", "doc-mobile", "national", "14040611", mobile,
            details, referrals, "patient", "14040610", new(2), 42);

    private static PrescriptionDetailInput Detail(DentalServiceFlag flag) =>
        new("14040610", new(71), "dose", new(3), "illness", flag, new Dictionary<string, object?> { ["opaque"] = 1 },
            9, "plan", 1, "repeat", new("service", "terminology"), 2, new(4), "11");
}
