EP.TAMIN E-PRESCRIPTION SERVICES

1. API PURPOSE

The EP.TAMIN API integration enables third-party HIS, EMR, clinic, pharmacy, laboratory, imaging, physiotherapy, and comprehensive healthcare systems to exchange electronic prescription data with the Social Security Insurance core services.

Main domains:

1. E-Prescription Writing
2. Pharmacy Prescription Dispensing
3. Paraclinic Prescription Dispensing
4. Eligibility Inquiry
5. Identity Verification
6. Reference Data Lookup
7. Prescription Tracking
8. Edit and Delete Operations
9. Warning, Price, and Limitation Display

2. BASE SYSTEMS

Prescription Writing Portal:
ep.tamin.ir

Dispensing Portal:
darman.tamin.ir

API Request and Onboarding Portal:
apiissue.tamin.ir

Official API documents, credentials, sandbox access, endpoint URLs, and Postman files must be received through the authorized API onboarding process.

3. COMMON API PRINCIPLES

Authentication:
Token-based authentication is required.

User Security:
Doctor and provider authentication may require two-step verification or OTP based on current security rules.

Patient Check:
Patient identity and treatment entitlement must be checked before prescription submission.

Request Format:
Usually JSON over HTTPS.

Transport:
HTTPS only.

Environment Separation:
Sandbox and production credentials must be separated.

Audit:
Every request and response must be logged with operation name, timestamp, status code, correlation ID, and local prescription ID.

Sensitive Data:
National ID, mobile number, prescription content, token, and credentials must be masked in logs.

4. COMMON HEADERS

Authorization:
Bearer {access_token}

Content-Type:
application/json

Accept:
application/json

Client-Id:
{issued_client_id}

Request-Id:
{unique_request_id}

Correlation-Id:
{local_trace_id}

5. COMMON RESPONSE STRUCTURE

Success response:

{
  "success": true,
  "statusCode": "string",
  "message": "string",
  "data": {},
  "trackingCode": "string",
  "correlationId": "string"
}

Error response:

{
  "success": false,
  "statusCode": "string",
  "message": "string",
  "errors": [],
  "correlationId": "string"
}

6. AUTHENTICATION SERVICES

6.1. Get Token

Purpose:
Receive access token for calling protected API services.

Suggested operation name:
GetToken

Input:
username
password
client_id
client_secret
otp, if required
provider_identifier

Output:
access_token
refresh_token, if supported
expires_in
token_type
user_roles
provider_info

Validation:
Reject invalid credentials.
Reject inactive provider.
Reject missing OTP when required.

6.2. Refresh Token

Purpose:
Renew expired access token without full login, if supported.

Suggested operation name:
RefreshToken

Input:
refresh_token
client_id

Output:
access_token
expires_in

6.3. Validate Token

Purpose:
Check whether the current token is still valid, if supported.

Suggested operation name:
ValidateToken

Input:
access_token

Output:
valid_flag
expires_at
user_info

7. IDENTITY AND ELIGIBILITY SERVICES

7.1. Identity Verification

Purpose:
Verify patient identity before issuing prescription.

Suggested operation name:
VerifyIdentity

Input:
national_id
birth_date, if required
mobile_number, if required
foreigner_identifier, if applicable

Output:
patient_name
patient_family
birth_date
gender
identity_status
patient_identifier

Business rules:
National ID must be valid.
Foreign patients may use the approved foreigner identifier.
Emergency cases may follow special workflow.

7.2. Treatment Entitlement Check

Purpose:
Check whether the patient has active treatment coverage.

Suggested operation name:
CheckEntitlement

Input:
national_id
provider_id
visit_date
service_type

Output:
eligible_flag
coverage_status
insurance_type
tracking_code
special_patient_flag
message

Business rules:
Normal electronic prescription submission should be blocked when entitlement is invalid.
Emergency workflows may continue according to policy.
The entitlement tracking code must be stored locally.

8. E-PRESCRIPTION WRITING SERVICES

8.1. Register Visit Prescription

Purpose:
Register a visit-only prescription or encounter.

Suggested operation name:
RegisterVisitPrescription

Input:
doctor_id
patient_national_id
visit_date
clinic_id
mobile_number
diagnosis_code, if required
description

Output:
prescription_id
tracking_code
status

8.2. Register Drug Prescription

Purpose:
Submit prescribed drug items.

Suggested operation name:
RegisterDrugPrescription

Input:
doctor_id
patient_national_id
visit_date
mobile_number
diagnosis_code, if required
drug_items[]

Drug item fields:
drug_code
quantity
dosage_instruction
frequency
duration
route
usage_note
repeat_count, if supported

Output:
prescription_id
tracking_code
accepted_items[]
rejected_items[]
warnings[]

8.3. Register Paraclinic Prescription

Purpose:
Submit laboratory, imaging, diagnostic, or other paraclinic orders.

Suggested operation name:
RegisterParaclinicPrescription

Input:
doctor_id
patient_national_id
visit_date
service_items[]

Service item fields:
service_code
service_group
quantity
effective_date
priority
description

Output:
prescription_id
tracking_code
accepted_items[]
rejected_items[]
warnings[]

8.4. Register Medical Service Prescription

Purpose:
Submit physician-provided services or other medical service orders.

Suggested operation name:
RegisterMedicalServicePrescription

Input:
doctor_id
patient_national_id
visit_date
service_items[]

Output:
prescription_id
tracking_code
status
warnings[]

8.5. Register Referral Service Prescription

Purpose:
Register referral to another provider, specialty, or service center.

Suggested operation name:
RegisterReferralPrescription

Input:
doctor_id
patient_national_id
target_specialty
target_provider_type
reason
visit_date
description

Output:
referral_prescription_id
tracking_code
status

8.6. Register Physiotherapy Prescription

Purpose:
Register physiotherapy prescription items.

Suggested operation name:
RegisterPhysiotherapyPrescription

Input:
doctor_id
patient_national_id
physiotherapy_items[]
session_count
effective_date
description

Output:
prescription_id
tracking_code
status
warnings[]

9. PRESCRIPTION QUERY SERVICES

9.1. Fetch Registered Prescription

Purpose:
Retrieve a previously registered prescription.

Suggested operation name:
GetRegisteredPrescription

Input:
prescription_id
tracking_code
patient_national_id

Output:
prescription_header
prescription_items[]
status
created_at
doctor_info
patient_info

9.2. Fetch Referral Prescription

Purpose:
Retrieve referral prescription details.

Suggested operation name:
GetReferralPrescription

Input:
referral_prescription_id
tracking_code
patient_national_id

Output:
referral_details
status
source_doctor
target_provider_type

9.3. Fetch Prescription List

Purpose:
Retrieve prescriptions by patient, doctor, date range, or status.

Suggested operation name:
GetPrescriptionList

Input:
doctor_id
patient_national_id
from_date
to_date
prescription_type
status

Output:
prescriptions[]

10. PRESCRIPTION MUTATION SERVICES

10.1. Edit Electronic Prescription

Purpose:
Edit an already registered electronic prescription where allowed.

Suggested operation name:
EditElectronicPrescription

Input:
prescription_id
tracking_code
edited_items[]
edit_reason

Output:
prescription_id
new_version
status
warnings[]

Business rules:
Only allowed fields may be changed.
The edit must be traceable.
Previous version must remain auditable.

10.2. Delete Electronic Prescription

Purpose:
Cancel or delete a registered electronic prescription where allowed.

Suggested operation name:
DeleteElectronicPrescription

Input:
prescription_id
tracking_code
delete_reason

Output:
prescription_id
status
deleted_at

Business rules:
Deletion must be logged.
Deleted prescriptions must not be silently removed from local audit history.

11. REFERENCE DATA SERVICES

11.1. Fetch Drug List

Purpose:
Retrieve official drug reference list.

Suggested operation name:
GetDrugList

Input:
search_text
drug_code
page
page_size
active_only

Output:
drug_list[]

Drug fields:
drug_code
drug_name
generic_name
form
strength
unit
active_flag

11.2. Fetch Service List

Purpose:
Retrieve official service reference list.

Suggested operation name:
GetServiceList

Input:
service_type
service_group
search_text
page
page_size
active_only

Output:
service_list[]

Service fields:
service_code
service_name
service_group
tariff_group
active_flag

11.3. Fetch Allowed Count

Purpose:
Show allowed quantity, count, or limitation for selected drug or service.

Suggested operation name:
GetAllowedCount

Input:
patient_national_id
item_code
item_type
doctor_id
date

Output:
allowed_count
used_count
remaining_count
limitation_message

11.4. Fetch Price

Purpose:
Display price, insurance share, patient share, or tariff data where available.

Suggested operation name:
GetPrice

Input:
item_code
item_type
quantity
patient_national_id
provider_id

Output:
total_price
insurance_share
patient_share
government_share
tariff_code
message

12. PHARMACY DISPENSING SERVICES

12.1. Get Token

Purpose:
Authenticate pharmacy system.

12.2. Check Entitlement

Purpose:
Check patient entitlement before dispensing.

12.3. Register Paper Prescription

Purpose:
Register paper prescription data when required.

12.4. Fetch Prescription List

Purpose:
Fetch available prescriptions for dispensing.

12.5. Fetch Prescription Details

Purpose:
Fetch prescription item-level details.

12.6. Refer Prescription to Doctor

Purpose:
Return or refer prescription to doctor for correction.

12.7. Check Prescription Warnings

Purpose:
Check dispensing warnings before final delivery.

12.8. Dispense Paper Prescription

Purpose:
Register delivery of paper prescription items.

12.9. Dispense Electronic Prescription

Purpose:
Register delivery of electronic prescription items.

12.10. Dispense With Warning

Purpose:
Register delivery when warning exists and workflow allows continuation.

12.11. Register Drug Authenticity Code

Purpose:
Register drug authenticity or tracking code.

12.12. Activate Drug Authenticity Code

Purpose:
Activate authenticity code after delivery.

12.13. Two-Step Electronic Dispensing

Purpose:
Support two-step delivery process where required.

12.14. Display Activated Barcode

Purpose:
Show activated prescription or drug barcode.

12.15. Display Price

Purpose:
Show price and share details.

12.16. Delete Dispensing Record

Purpose:
Delete or cancel dispensing operation where allowed.

13. PARACLINIC DISPENSING SERVICES

13.1. Get Token

Purpose:
Authenticate paraclinic provider system.

13.2. Check Entitlement

Purpose:
Check patient treatment entitlement.

13.3. Register Paper Prescription

Purpose:
Register paper prescription, optional depending on provider type and workflow.

13.4. Fetch Prescription List

Purpose:
Fetch paraclinic prescriptions waiting for service delivery.

13.5. Fetch Prescription Details

Purpose:
Fetch item-level service details.

13.6. Provide Paper Prescription Service

Purpose:
Register delivery of service from paper prescription.

13.7. Provide Electronic Prescription Service

Purpose:
Register delivery of electronic paraclinic service.

13.8. Provide Service With Warning

Purpose:
Register delivery where warnings exist and continuation is allowed.

13.9. Display Price

Purpose:
Show tariff, insurance share, and patient share.

13.10. Delete Service Delivery Record

Purpose:
Delete or cancel service delivery where allowed.

14. WARNING SERVICES

14.1. Check Prescription Warning

Purpose:
Return warnings before prescription registration or dispensing.

Suggested operation name:
CheckPrescriptionWarning

Input:
patient_national_id
doctor_id
prescription_items[]

Output:
warnings[]

Warning fields:
warning_code
warning_type
severity
message
can_continue_flag
requires_confirmation_flag

15. COMMON STATUS VALUES

Draft:
Created locally but not submitted.

Submitted:
Sent to official API.

Accepted:
Accepted by official API.

Rejected:
Rejected by official API.

Warning:
Accepted conditionally or requires user attention.

Edited:
Changed after original registration.

Deleted:
Deleted or cancelled where allowed.

PendingSync:
Waiting for retry after temporary failure.

Failed:
Submission failed and requires support review.

16. COMMON ERROR CATEGORIES

AuthenticationError:
Invalid credentials, expired token, missing OTP.

AuthorizationError:
Provider or user not allowed to perform operation.

IdentityError:
Patient identity could not be verified.

EntitlementError:
Patient does not have valid treatment entitlement.

ValidationError:
Required field, invalid code, invalid quantity, or invalid date.

BusinessRuleError:
Prescription violates insurance rule or medical service rule.

DuplicateSubmissionRisk:
Timeout or retry may have already created prescription.

TemporaryServiceError:
External service unavailable.

UnknownError:
Unhandled or unmapped error.

17. MINIMUM LOCAL LOGGING FIELDS

api_transaction_id
operation_name
local_prescription_id
official_prescription_id
request_id
correlation_id
doctor_id
patient_national_id_masked
request_time
response_time
response_status
response_code
success_flag
error_category
error_message_masked

18. MINIMUM LOCAL DATABASE ENTITIES

Doctor
Patient
EligibilityInquiry
Prescription
PrescriptionItem
PrescriptionApiTransaction
ReferenceDrug
ReferenceService
PrescriptionWarning
DispensingRecord
ParaclinicDeliveryRecord
AuditLog
OutageFallbackRecord

19. IMPLEMENTATION NOTES

1. Never call the EP.TAMIN API directly from frontend code.
2. Implement a backend adapter layer for all external calls.
3. Keep external API DTOs separate from internal domain models.
4. Store official tracking codes and prescription IDs.
5. Apply idempotency to prescription submission.
6. Retry only safe operations.
7. Before retrying after timeout, fetch prescription status to prevent duplicate registration.
8. Cache reference data, but keep version history.
9. Mask sensitive data in logs.
10. Keep all create, edit, delete, print, and dispense actions auditable.

20. DOCUMENTATION SECTIONS TO COMPLETE AFTER RECEIVING OFFICIAL API PACKAGE

1. Exact base URLs for sandbox and production.
2. Exact authentication flow.
3. Exact OTP flow.
4. Endpoint paths.
5. HTTP methods.
6. Request DTOs.
7. Response DTOs.
8. Error code catalog.
9. Field validation rules.
10. Prescription type codes.
11. Service type codes.
12. Drug and service reference data schema.
13. Rate limits.
14. Retry policy recommended by Tamin.
15. Certification test scenarios.
16. Postman collection.
17. Production go-live checklist.
