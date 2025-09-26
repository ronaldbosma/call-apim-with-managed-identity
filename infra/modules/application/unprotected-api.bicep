//=============================================================================
// Unprotected API in API Management
//=============================================================================

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

resource oauthTargetResourceNamedValue 'Microsoft.ApiManagement/service/namedValues@2024-06-01-preview' = {
  name: 'oauth-target-resource'
  parent: apiManagementService
  properties: {
    displayName: 'oauth-target-resource'
    value: oauthTargetResource
  }
}

// Backends

resource localhostBackend 'Microsoft.ApiManagement/service/backends@2024-06-01-preview' = {
  parent: apiManagementService
  name: 'localhost'
  properties: {
    description: 'The localhost backend. Can be used to call other APIs hosted in the same API Management instance.'

    // Note: This configuration uses the public gateway URL for the backend.
    // For APIM instances running inside a VNet, you would typically use https://localhost as the backend URL.
    url: apiManagementService.properties.gatewayUrl
    protocol: 'http'
    
    // Note: The Host header configuration is only necessary when the backend URL is set to https://localhost.
    // For public gateway URLs, this configuration can be omitted.
    credentials : {
      header: {
        Host: [ parseUri(apiManagementService.properties.gatewayUrl).host ]
      }
    }
    
    tls: {
      validateCertificateChain: true
      validateCertificateName: true
    }
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

    dependsOn: [
      oauthTargetResourceNamedValue
      localhostBackend
    ]
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
}
