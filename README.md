# Amazon AIA SDK

This SDK provides reference code for a vendor to implement an Amazon Instant Access (AIA) endpoint. There are 3 visual studio projects included:

  - DTASDK - Contains request/response objects, and signing logic
  - DTASDKTest - Test project for signing logic
  - DTASDKWeb - An ASP.Net WebApi project with controllers that handle AIA requests

### Dependencies and Building

[Paket] is used to manage and install nuget packages. The main dependencies are ASP.Net WebApi for DTASDKWeb, and Moq for testing. Building DTASDK.sln with Visual Studio or msbuild from a developer command prompt will install the required dependencies via paket for each project.

### Getting Started

Implementing your AIA endpoint is similar to the the [Java Sdk]. An AccountLinkingController and PurchaseController are provided in the DTASDKWeb project that must be extended with your implementation for the required methods and credential store. Configure your routes in WebApiConfig.cs or use Attribute based routing on your controller implementation.

While this implementation uses WebApi, you can use any web framework to implement your endpoint using the same pattern used in the InstantAccessController and classes provided in the DTASDK project. 

   [Paket]: <https://fsprojects.github.io/Paket/index.html>
   [Java Sdk]: <https://s3-us-west-2.amazonaws.com/dtg-docs/java/index.html>