# Call API Management with Managed Identity - Demo

This demo shows how to call Azure API Management (APIM) from Azure Functions and Logic Apps using managed identity authentication with OAuth. It also shows how one APIM API can securely call another APIM API using its managed identity, enabling secret-free, secure authentication between Azure services.

The template deploys an Azure API Management service with two APIs: a protected backend API secured with OAuth and an unprotected public API that calls the protected API using API Management's system-assigned managed identity. It also deploys a .NET 9 Azure Function App and an Azure Logic App (Standard), both configured to call the protected API using their own system-assigned managed identities. Supporting resources such as Application Insights, a Log Analytics workspace, a Storage Account and Entra ID app registration are also provisioned. See the following diagram for an overview:

![Overview](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/diagrams-overview.png)

## 1. What resources get deployed

The following resources are deployed in a resource group in your Azure subscription:

![Deployed Resources](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/deployed-resources.png)

The following app registration is created in your Entra ID tenant:

![Deployed App Registrations](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/deployed-app-registrations.png)

The deployed resources follow the naming convention: `<resource-type>-<environment-name>-<region>-<instance>`.


## 2. What you can demo after deployment

### Review the configuration

Before diving into the scenarios, let's understand how managed identity authentication works in this setup.

**Protected API policy**

The protected API uses the `validate-azure-ad-token` policy to enforce OAuth authentication. This policy:

You can find this policy in [protected-api.xml](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/infra/modules/application/protected-api.xml).

**App registration**

The [apim-app-registration.bicep](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/infra/modules/entra-id/apim-app-registration.bicep) file creates an Entra ID app registration for the protected API. 
This app registration defines the Application ID URI (used as the OAuth audience) and the available app roles (`Sample.Read`, `Sample.Write`, `Sample.Delete`).

**Role assignment configuration**

The [assign-app-roles.bicep](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/infra/modules/entra-id/assign-app-roles.bicep) file assigns the `Sample.Read` and `Sample.Write` roles to the various managed identities:
- API Management system-assigned managed identity
- Function App system-assigned managed identity  
- Logic App system-assigned managed identity

Note that none of the managed identities receive the `Sample.Delete` role, which is why DELETE operations will fail with 401 Unauthorized.


### Scenario 1: Call unprotected API that calls protected API using APIM managed identity

**Execute the scenario**

1. Open [tests.apim.http](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/tests/tests.apim.http) in Visual Studio Code.
1. Replace `<your-api-management-service-name>` with the name of your API Management service.
1. Execute the requests in the file:
   - GET request - Should succeed (200 OK) - the protected API requires `Sample.Read` role
   - POST request - Should succeed (200 OK) - the protected API requires `Sample.Write` role
   - DELETE request - Should fail (401 Unauthorized) - the protected API requires `Sample.Delete` role (not assigned)

The HTTP method used on the unprotected API directly matches the operation called on the protected API, and the protected API determines the required role based on the HTTP method.

Here's a sequence diagram that shows the flow for all three requests (GET, POST, DELETE):

![Sequence Diagram - APIM to APIM](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/diagrams-apim-to-apim.png)

Note how the access token is retrieved during the first GET request and then cached for subsequent POST and DELETE requests. If you execute the GET and POST requests multiple times, you'll notice that the `IssuedAt` value in the response doesn't change, demonstrating that API Management automatically caches access tokens for improved performance.

**Review the policy implementation**

The unprotected API uses the `authentication-managed-identity` policy to automatically obtain an access token using API Management's system-assigned managed identity. You can see this simple but powerful implementation in [unprotected-api.xml](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/infra/modules/application/unprotected-api.xml).

The policy:
- Uses `set-backend-service` to forward requests to the protected API
- Uses `authentication-managed-identity` to authenticate with the managed identity
- The `oauth-target-resource` named value is configured with the `Application ID URI` of the app registration so the access token is requested for the correct protected resource
- Adds debugging headers to show the backend request URL


### Scenario 2: Azure Function calls protected API using its managed identity

**Execute the scenario**

1. Open [tests.function.http](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/tests/tests.function.http) in Visual Studio Code.
2. Replace `<your-function-app-name>` with your Function App hostname.
3. Execute the requests in the file:
   - GET request - Should succeed (200 OK) - the protected API requires `Sample.Read` role
   - POST request - Should succeed (200 OK) - the protected API requires `Sample.Write` role
   - DELETE request - Should fail (401 Unauthorized) - the protected API requires `Sample.Delete` role (not assigned)

The HTTP method used to call the Azure Function directly matches the operation called on the protected API, and the protected API determines the required role based on the HTTP method.

Here's a sequence diagram that shows the flow for all three requests (GET, POST, DELETE):

![Sequence Diagram - Function to APIM](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/diagrams-function-to-apim.png)

Note how the access token is retrieved during the first GET request and then cached for subsequent POST and DELETE requests. If you execute the GET and POST requests multiple times, you'll notice that the `IssuedAt` value in the response doesn't change, demonstrating that `DefaultAzureCredential` automatically handles token caching and renewal.

**Review the implementation**

The Azure Function uses a custom `HttpMessageHandler` to automatically add OAuth tokens to outgoing requests:

1. [AzureCredentialsAuthorizationHandler.cs](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/src/functionApp/FunctionApp/AzureCredentialsAuthorizationHandler.cs) - Contains the logic to retrieve access tokens using `DefaultAzureCredential`. This handler:
   - Uses `DefaultAzureCredential` to authenticate with the managed identity
   - Requests an access token for the configured OAuth target resource, which is the `Application ID URI` of the app registration
   - Adds the token to the Authorization header of outgoing requests

1. [CallProtectedApiFunction.cs](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/src/functionApp/FunctionApp/CallProtectedApiFunction.cs) - The Azure Function that calls the protected API. It:
   - Accepts GET, POST, and DELETE requests
   - Uses the configured HttpClient (which includes the authorization handler)
   - Forwards the request to the `/protected` endpoint
   - Returns the response from the protected API

1. [ServiceCollectionExtensions.cs](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/src/functionApp/FunctionApp/ServiceCollectionExtensions.cs) - The `RegisterDependencies` extension method configures the dependency injection container. It:
   - Registers the `AzureCredentialsAuthorizationHandler` as a scoped service
   - Configures an HttpClient named "apim" with the API Management gateway URL
   - Adds the `AzureCredentialsAuthorizationHandler` to the HttpClient pipeline using `AddHttpMessageHandler`
   - This ensures that every request made with this HttpClient automatically includes the OAuth token


### Scenario 3: Logic App workflow calls protected API using its managed identity

**Execute the scenario**

1. Open [tests.workflow.http](https://github.com/ronaldbosma/call-apim-with-managed-identity/blob/main/tests/tests.workflow.http) in Visual Studio Code.
2. Replace `<your-call-protected-api-workflow-url>` with your Logic App workflow URL.  
   To get the Logic App workflow URL:
   - Navigate to your Logic App resource in the Azure portal
   - Open the `call-protected-api-workflow` workflow
   - Click on the HTTP trigger (first step in the workflow)
   - Copy the value of the "HTTP URL" from the trigger details
   - Use this URL to replace `<your-call-protected-api-workflow-url>` in the file
3. Execute the requests in the file:
   - POST with `"httpMethod": "GET"` - Should succeed (200 OK) - the protected API requires `Sample.Read` role
   - POST with `"httpMethod": "POST"` - Should succeed (200 OK) - the protected API requires `Sample.Write` role
   - POST with `"httpMethod": "DELETE"` - Should fail (401 Unauthorized) - the protected API requires `Sample.Delete` role (not assigned)

The `httpMethod` property in the request body determines which HTTP method the workflow uses to call the protected API, and the protected API determines the required role based on that HTTP method.

Here's a sequence diagram that shows the flow for all three requests (GET, POST, DELETE):

![Sequence Diagram - Workflow to APIM](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/diagrams-workflow-to-apim.png)

Note how the access token is retrieved during the first GET request and then cached for subsequent POST and DELETE requests. If you execute the GET and POST requests multiple times, you'll notice that the `IssuedAt` value in the response doesn't change, demonstrating that Logic Apps automatically cache and renew managed identity tokens.

**Review the workflow configuration**

Navigate to your Logic App resource in the Azure portal and open the `call-protected-api-workflow` workflow. You'll see a workflow that:
- Receives the HTTP method as input
- Makes an HTTP request to the protected API
- Uses the Logic App's system-assigned managed identity for authentication

In the HTTP action configuration, you'll see:
- The authentication type is set to "Managed Identity"  
- The resource/audience is configured to match the protected API's expected audience, which is the `Application ID URI` of the app registration
- The Logic App automatically handles token acquisition and renewal
