//=============================================================================
// API Management App Registration with App roles & Service Principal
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Extensions
//=============================================================================

extension microsoftGraphV1

//=============================================================================
// Imports
//=============================================================================

import * as helpers from '../../functions/helpers.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('The ID of the tenant')
param tenantId string

@description('The tags to associate with the resource')
param tags object

@description('The name of the API Management app registration in Entra ID')
param name string

@description('The identifier URI for the API Management app registration')
param identifierUri string

@description('If true, allows API access for users by adding a scope to the API Management app registration.')
param allowApiAccessForUsers bool

//=============================================================================
// Variables
//=============================================================================

var appRoles = [
  {
    name: 'Sample.Read'
    description: 'Sample read application role'
  }
  {
    name: 'Sample.Write'
    description: 'Sample write application role'
  }
  {
    name: 'Sample.Delete'
    description: 'Sample delete application role'
  }
]

var apiAccessScope = 'API.Access'

//=============================================================================
// Resources
//=============================================================================

resource apimAppRegistration 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: name
  displayName: name

  identifierUris: [ identifierUri ]

  api: {
    requestedAccessTokenVersion: 2 // Issue OAuth v2.0 access tokens

    // Add OAuth2 permission scope so users can request an access token to access the API, if allowApiAccessForUsers is true
    oauth2PermissionScopes: allowApiAccessForUsers ? [
      {
        id: guid(tenantId, name, apiAccessScope)
        adminConsentDescription: 'Allows API access for users'
        adminConsentDisplayName: apiAccessScope
        isEnabled: true
        type: 'User'
        userConsentDescription: null
        userConsentDisplayName: null
        value: apiAccessScope
      }
    ] : []
  }

  appRoles: [for role in appRoles: {
    id: guid(tenantId, name, role.name) // Create an deterministic ID for the app role based on the tenant ID, app name and role name
    description: role.description
    displayName: role.name
    value: role.name
    allowedMemberTypes: [ 'Application' ]
    isEnabled: true
  }]
  
  // Add a 'HideApp' tag to hide the app from the end-users in the My Apps portal
  tags: concat(helpers.flattenTags(tags), ['HideApp'])
}

resource apimServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apimAppRegistration.appId
  appRoleAssignmentRequired: true // When true, clients must have an app role assigned in order to retrieve an access token
}

// Get Azure CLI service principal, created if not exists
@onlyIfNotExists()
resource azureCliServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = if (allowApiAccessForUsers) {
  appId: '04b07795-8ddb-461a-bbee-02f9e1bf7b46'
}

// Add OAuth2 permission grant to allow the Azure CLI service principal to access the API Management app registration impersonating a user
// NOTE: The user still needs to be granted app roles in order to access the API
resource oauth2PermissionGrantForAzureCli 'Microsoft.Graph/oauth2PermissionGrants@v1.0' = if (allowApiAccessForUsers) {
  clientId: azureCliServicePrincipal!.id
  resourceId: apimServicePrincipal.id
  consentType: 'AllPrincipals'
  scope: apiAccessScope
}

//=============================================================================
// Outputs
//=============================================================================

output appId string = apimAppRegistration.appId
