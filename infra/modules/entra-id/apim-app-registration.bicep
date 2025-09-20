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

//=============================================================================
// Resources
//=============================================================================

resource apimAppRegistration 'Microsoft.Graph/applications@v1.0' = {
  tags: helpers.flattenTags(tags)
  
  uniqueName: name
  displayName: name

  identifierUris: [ identifierUri ]

  api: {
    requestedAccessTokenVersion: 2 // Issue OAuth v2.0 access tokens
  }

  appRoles: [for role in appRoles: {
    id: guid(tenantId, name, role.name) // Create an deterministic ID for the app role based on the tenant ID, app name and role name
    description: role.description
    displayName: role.name
    value: role.name
    allowedMemberTypes: [ 'Application' ]
    isEnabled: true
  }]

  owners: {
    relationships: [
      deployer().objectId
    ]
  }
}

resource apimServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apimAppRegistration.appId
  appRoleAssignmentRequired: true // When true, clients must have an app role assigned in order to retrieve an access token
}

//=============================================================================
// Outputs
//=============================================================================

output appId string = apimAppRegistration.appId
