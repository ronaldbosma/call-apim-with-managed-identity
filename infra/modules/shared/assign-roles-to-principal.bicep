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

@description('The name of the App Insights instance on which to assign role')
param appInsightsName string

//=============================================================================
// Variables
//=============================================================================

var monitoringMetricsPublisher string = 'Monitoring Metrics Publisher'

//=============================================================================
// Existing Resources
//=============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

//=============================================================================
// Resources
//=============================================================================

// Assign role Application Insights to the principal

resource assignAppInsightRolesToPrincipal 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(principalId, appInsights.id, roleDefinitions(monitoringMetricsPublisher).id)
  scope: appInsights
  properties: {
    #disable-next-line use-resource-id-functions
    roleDefinitionId: roleDefinitions(monitoringMetricsPublisher).id
    principalId: principalId
    principalType: principalType
  }
}
