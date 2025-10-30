//=============================================================================
// Call API Management with Managed Identity
// Source: https://github.com/ronaldbosma/call-apim-with-managed-identity
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Imports
//=============================================================================

import { getResourceName, getInstanceId } from './functions/naming-conventions.bicep'

//=============================================================================
// Parameters
//=============================================================================

@minLength(1)
@description('Location to use for all resources')
param location string

@minLength(1)
@maxLength(32)
@description('The name of the environment to deploy to')
param environmentName string

@maxLength(5) // The maximum length of the storage account name and key vault name is 24 characters. To prevent errors the instance name should be short.
@description('The instance that will be added to the deployed resources names to make them unique. Will be generated if not provided.')
param instance string = ''

@description('If true, allows API access for users by adding a scope to the API Management app registration.')
param allowApiAccessForUsers bool

//=============================================================================
// Variables
//=============================================================================

// Determine the instance id based on the provided instance or by generating a new one
var instanceId = getInstanceId(environmentName, location, instance)

var resourceGroupName = getResourceName('resourceGroup', environmentName, location, instanceId)

var apiManagementSettings = {
  serviceName: getResourceName('apiManagement', environmentName, location, instanceId)
  publisherName: 'admin@example.org'
  publisherEmail: 'admin@example.org'
  appRegistrationName: getResourceName('appRegistration', environmentName, location, 'apim-${instanceId}')
  appRegistrationIdentifierUri: 'api://${getResourceName('apiManagement', environmentName, location, instanceId)}'
}

var appInsightsSettings = {
  appInsightsName: getResourceName('applicationInsights', environmentName, location, instanceId)
  logAnalyticsWorkspaceName: getResourceName('logAnalyticsWorkspace', environmentName, location, instanceId)
  retentionInDays: 30
}

var functionAppSettings = {
  functionAppName: getResourceName('functionApp', environmentName, location, instanceId)
  appServicePlanName: getResourceName('appServicePlan', environmentName, location, 'functionapp-${instanceId}')
  netFrameworkVersion: 'v9.0'
}

var logicAppSettings = {
  logicAppName: getResourceName('logicApp', environmentName, location, instanceId)
  appServicePlanName: getResourceName('appServicePlan', environmentName, location, 'logicapp-${instanceId}')
  netFrameworkVersion: 'v8.0'
}

var storageAccountName = getResourceName('storageAccount', environmentName, location, instanceId)

// Generate a unique ID for the azd environment so we can identity the Entra ID resources created for this environment
// The environment name is not unique enough as multiple environments can have the same name in different subscriptions, regions, etc.
var azdEnvironmentId = getResourceName('azdEnvironment', environmentName, location, instanceId)

var tags = {
  'azd-env-name': environmentName
  'azd-env-id': azdEnvironmentId
  'azd-template': 'ronaldbosma/call-apim-with-managed-identity'

  // The SecurityControl tag is added to Trainer Demo Deploy projects so resources can run in MTT managed subscriptions without being blocked by default security policies.
  // DO NOT USE this tag in production or customer subscriptions.
  SecurityControl: 'Ignore'
}

//=============================================================================
// Resources
//=============================================================================

module apimAppRegistration 'modules/entra-id/apim-app-registration.bicep' = {
  params: {
    tenantId: subscription().tenantId
    tags: tags
    name: apiManagementSettings.appRegistrationName
    identifierUri: apiManagementSettings.appRegistrationIdentifierUri
    allowApiAccessForUsers: allowApiAccessForUsers
  }
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module storageAccount 'modules/services/storage-account.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    storageAccountName: storageAccountName
  }
}

module appInsights 'modules/services/app-insights.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    appInsightsSettings: appInsightsSettings
  }
}

module apiManagement 'modules/services/api-management.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
  }
  dependsOn: [
    appInsights
  ]
}

module functionApp 'modules/services/function-app.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    functionAppSettings: functionAppSettings
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
    storageAccountName: storageAccountName
  }
  dependsOn: [
    appInsights
    storageAccount
  ]
}

module logicApp 'modules/services/logic-app.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    logicAppSettings: logicAppSettings
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
    storageAccountName: storageAccountName
  }
  dependsOn: [
    appInsights
    storageAccount
  ]
}

// Assign app roles to the deployer (the user or pipeline executing the deployment) so they can call the Protected API
// These are configured for integration tests to demo the scenario where a user or pipeline calls on OAuth-Protected API using their own identity
module assignAppRolesToDeployer 'modules/entra-id/assign-app-roles.bicep' = {
  scope: subscription()
  params: {
    apimAppRegistrationName: apiManagementSettings.appRegistrationName
    clientServicePrincipalId: deployer().objectId
  }
  
  dependsOn: [
    apimAppRegistration
    // Assignment of the app roles fails if we do this immediately after creating the app registration.
    // By adding a dependency on the API Management module, we ensure that enough time has passed for the app role assignments to succeed.
    apiManagement
  ]
}

//=============================================================================
// Application Resources
//=============================================================================

module protectedApi 'modules/application/protected-api.bicep' = {
  scope: resourceGroup
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
    tenantId: subscription().tenantId
    oauthAudience: apimAppRegistration.outputs.appId
  }
  dependsOn: [
    apiManagement
  ]
}

module unprotectedApi 'modules/application/unprotected-api.bicep' = {
  scope: resourceGroup
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
    oauthTargetResource: apiManagementSettings.appRegistrationIdentifierUri
  }
  dependsOn: [
    apiManagement
  ]
}

//=============================================================================
// Outputs
//=============================================================================

// Return the azd environment id
output AZURE_ENV_ID string = azdEnvironmentId

// Return names of the Entra ID resources
output ENTRA_ID_APIM_APP_REGISTRATION_NAME string = apiManagementSettings.appRegistrationName
output ENTRA_ID_APIM_APP_REGISTRATION_IDENTIFIER_URI string = apiManagementSettings.appRegistrationIdentifierUri

// Return the names of the resources
output AZURE_API_MANAGEMENT_NAME string = apiManagementSettings.serviceName
output AZURE_APPLICATION_INSIGHTS_NAME string = appInsightsSettings.appInsightsName
output AZURE_FUNCTION_APP_NAME string = functionAppSettings.functionAppName
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = appInsightsSettings.logAnalyticsWorkspaceName
output AZURE_LOGIC_APP_NAME string = logicAppSettings.logicAppName
output AZURE_RESOURCE_GROUP string = resourceGroupName
output AZURE_STORAGE_ACCOUNT_NAME string = storageAccountName

// Return resource endpoints
output AZURE_API_MANAGEMENT_GATEWAY_URL string = apiManagement.outputs.gatewayUrl
output AZURE_FUNCTION_APP_ENDPOINT string = functionApp.outputs.endpoint
output AZURE_LOGIC_APP_ENDPOINT string = logicApp.outputs.endpoint

// Return whether API access for users is allowed
output ALLOW_API_ACCESS_FOR_USERS bool = allowApiAccessForUsers
