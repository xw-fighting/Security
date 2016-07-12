# How to set up the sample locally

The OpenIdConnect sample support multilpe authentication providers. In this instruction, we will explore how to set up this sample with both Azure Active Directory and Google Firebase Authentication

## Determine your development environment and a few key variables

If you are running the application with Visual Studio, launch settings are preconfigured in `launchSettings.json`. Your web application's URL is __https://localhost:44318__

If you are running the application from command line or terminal, the launch settings are not binding to the `launch.json`. In this case the web application`s URL is __http://localhost:5000__. In addition to the different sample URL, environment variable ASPNETCORE_ENVIRONMENT should be set to DEVELOPMENT to enable user secret.

## Configure the Authorization server

### Configure with Azure Active Directory

1. Set up a new Azure Active Directory (AAD) in your Azure Subscription.
2. Open the newly created AAD in Azure web portal
3. Navigate to the Applications tab
4. Add a new Application to the AAD. Set the "Sign-on URL" to sample application's URL.
5. Naigate to the Application, and click the Configure tab.
6. Find and save the "Client Id".
7. Add a new key in the "Keys" section. Save value of the key, which is the "Client Secret".
8. Click the "View Endpoints" on the drawer, a dialog will shows six endpoint URLs. Copy the "OAuth 2.0 Authorization Endpoint" to a text editor and remove the "/oauth2/authorize" from the string. The remaining part is the __authority URL__. It looks like __https://login.microsoftonline.com/<guid>__

### Configure with Google Firebase Authentication

1. Create a new project through [Google APIs](console.developers.google.com)
2. In the sidebar choose "Credentials"
3. Navigate to "OAuth consent screen" tab, fill in the project name and save.
4. Navigate to "Credentials" tab. Click "Create credentials". Choose "OAuth client ID". 
5. Select "Web application" as the application type. Fill in the "Authorized redirect URIs" with __https://localhost:44318/signin-oidc__ (If you're running from command line, use __http://localhost:5000/signin-oidc__ instead).
6. Save the "Client ID" and "Client Secret" shown in the dialog.
7. Save the "Authority URL" for Google Authentication is __https://accounts.google.com/

## Configure the sample application

1. Restore the application.
2. Set user secrets

```
dotnet user-secrets set oidc:clientid <Client Id>
dotnet user-secrets set oidc:clientsecret <Client Secret>
dotnet user-secrets set oidc:authority <Authority URL>
```

