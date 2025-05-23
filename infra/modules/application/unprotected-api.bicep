//=============================================================================
// Unprotected API in API Management
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import * as helpers from '../../functions/helpers.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the API Management service')
param apiManagementServiceName string

@description('The OAuth target resource for which a JWT token is requested by the APIM managed identity')
param oauthTargetResource string

//=============================================================================
// Existing resources
//=============================================================================

resource apiManagementService 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apiManagementServiceName
}

//=============================================================================
// Resources
//=============================================================================

// Named Values

resource apimGatewayUrlNamedValue 'Microsoft.ApiManagement/service/namedValues@2024-06-01-preview' = {
  name: 'apim-gateway-url'
  parent: apiManagementService
  properties: {
    displayName: 'apim-gateway-url'
    value: helpers.getApiManagementGatewayUrl(apiManagementServiceName)
  }
}

resource oauthTargetResourceNamedValue 'Microsoft.ApiManagement/service/namedValues@2024-06-01-preview' = {
  name: 'oauth-target-resource'
  parent: apiManagementService
  properties: {
    displayName: 'oauth-target-resource'
    value: oauthTargetResource
  }
}

// API

resource unprotectedApi 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  name: 'unprotected-api'
  parent: apiManagementService
  properties: {
    displayName: 'Unprotected API'
    path: 'unprotected'
    protocols: [ 
      'https' 
    ]
    subscriptionRequired: false // API is unprotected
  }
  
  resource policies 'policies' = {
    name: 'policy'
    properties: {
      format: 'rawxml'
      value: loadTextContent('unprotected-api.xml')
    }
  }

  resource getOperation 'operations' = {
    name: 'get'
    properties: {
      displayName: 'Get'
      method: 'GET'
      urlTemplate: '/'
    }
  }

  resource postOperation 'operations' = {
    name: 'post'
    properties: {
      displayName: 'Post'
      method: 'POST'
      urlTemplate: '/'
    }
  }

  resource deleteOperation 'operations' = {
    name: 'delete'
    properties: {
      displayName: 'Delete'
      method: 'DELETE'
      urlTemplate: '/'
    }
  }

  dependsOn: [
    apimGatewayUrlNamedValue
    oauthTargetResourceNamedValue
  ]
}
