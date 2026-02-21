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

var monitoringMetricsPublisher string = '3913510d-42f4-4e42-8a64-420c390055eb' // Monitoring Metrics Publisher

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
  name: guid(principalId, appInsights.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisher))
  scope: appInsights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisher)
    principalId: principalId
    principalType: principalType
  }
}
