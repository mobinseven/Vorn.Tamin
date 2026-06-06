namespace Vorn.Tamin.Kiota;

/// <summary>Identifies friendly operations whose provider path is not represented by generated Kiota request builders.</summary>
internal enum TaminGatewayRoute
{
    VerifyIdentity,
    CheckEntitlement,
    AllowedCount,
    Price,
    PrescriptionDetail,
    PrescriptionEdit,
    PrescriptionRemove,
    PrescriptionWarning,
    PharmacyCheckEntitlement,
    PharmacyRegisterPaper,
    PharmacyPrescriptionList,
    PharmacyPrescriptionDetails,
    PharmacyReferToDoctor,
    PharmacyCheckWarnings,
    PharmacyDispensePaper,
    PharmacyDispenseElectronic,
    PharmacyDispenseWithWarning,
    PharmacyRegisterAuthenticityCode,
    PharmacyActivateAuthenticityCode,
    PharmacyTwoStepDispense,
    PharmacyActivatedBarcode,
    PharmacyPrice,
    PharmacyDeleteDispensing,
    ParaclinicCheckEntitlement,
    ParaclinicRegisterPaper,
    ParaclinicPrescriptionList,
    ParaclinicPrescriptionDetails,
    ParaclinicProvidePaper,
    ParaclinicProvideElectronic,
    ParaclinicProvideWithWarning,
    ParaclinicPrice,
    ParaclinicDeleteDelivery
}
