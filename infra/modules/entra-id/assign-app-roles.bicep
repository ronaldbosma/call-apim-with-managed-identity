//=============================================================================
// Assign App Roles to the Client Service Principal
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Extensions
//=============================================================================

extension microsoftGraphV1

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the API Management app registration')
param apimAppRegistrationName string

@description('The id of the service principal that will be assigned the app roles')
param clientServicePrincipalId string

//=============================================================================
// Variables
//=============================================================================

var rolesToAssign string[] = [
  'Sample.Read'
  'Sample.Write'
]

//=============================================================================
// Existing Resources
//=============================================================================

resource apimAppRegistration 'Microsoft.Graph/applications@v1.0' existing = {
  uniqueName: apimAppRegistrationName
}

resource apimServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' existing = {
  appId: apimAppRegistration.appId
}

//=============================================================================
// Functions
//=============================================================================

func getAppRoleIdByValue(appRoles array, value string) string =>
  first(filter(appRoles, (role) => role.value == value)).id

//=============================================================================
// Resources
//=============================================================================

resource assignAppRole 'Microsoft.Graph/appRoleAssignedTo@v1.0' = [for role in rolesToAssign: {
  resourceId: apimServicePrincipal.id
  appRoleId: getAppRoleIdByValue(apimAppRegistration.appRoles, role)
  principalId: clientServicePrincipalId
}]
