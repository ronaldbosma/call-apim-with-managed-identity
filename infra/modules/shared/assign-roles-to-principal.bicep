//=============================================================================
// Assign roles to principal on Application Insights
//=============================================================================

//=============================================================================
// Parameters
//=============================================================================

@description('The id of the principal that will be assigned the role')
param principalId string

@description('The type of the principal that will be assigned the role')
param principalType string?

@description('The flag to determine if the principal is an admin or not')
param isAdmin bool = false

@description('The name of the App Insights instance on which to assign role')
param appInsightsName string

@description('The name of the Storage Account on which to assign roles')
param storageAccountName string

//=============================================================================
// Variables
//=============================================================================

var storageAccountRoleNames string[] = [
  'Storage Blob Data Contributor'
  isAdmin
    ? 'Storage File Data Privileged Contributor' // is able to browse file shares in Azure Portal
    : 'Storage File Data SMB Share Contributor'
  'Storage Queue Data Contributor'
  'Storage Table Data Contributor'
]

var monitoringMetricsPublisherRoleName string = 'Monitoring Metrics Publisher'

//=============================================================================
// Existing Resources
//=============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2026-09-01' existing = {
  name: storageAccountName
}

//=============================================================================
// Resources
//=============================================================================

// Assign role Application Insights to the principal

resource assignAppInsightRolesToPrincipal 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(principalId, appInsights.id, roleDefinitions(monitoringMetricsPublisherRoleName).id)
  scope: appInsights
  properties: {
    roleDefinitionId: roleDefinitions(monitoringMetricsPublisherRoleName).id
    principalId: principalId
    principalType: principalType
  }
}

// Assign roles on Storage Account to the principal

resource assignRolesOnStorageAccountToPrincipal 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for role in storageAccountRoleNames: {
    name: guid(principalId, storageAccount.id, roleDefinitions(role).id)
    scope: storageAccount
    properties: {
      roleDefinitionId: roleDefinitions(role).id
      principalId: principalId
      principalType: principalType
    }
  }
]
