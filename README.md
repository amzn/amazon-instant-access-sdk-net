# Amazon AIA SDK

This repository contains reference code for implementing an Amazon Instant Access (AIA) endpoint. There projects included are:

  - DTASDK - Contains request/response objects, and signing logic
  - DTASDKTest - Test project for signing logic
  - DTASDKWeb - An ASP.Net core project with controllers that handle AIA requests

## Building and Running

  - Restore packages with `dotnet restore` and then build with `dotnet build`.
  - Run the server with `dotnet run --project ./DTASDKWeb/DTASDKWeb.csproj`

[Paket] is used to manage and restore dependencies.

## Getting Started

Implementing your AIA endpoint is similar to the the [Java Sdk]. An `AccountLinkingController` and `PurchaseController` are provided in the DTASDKWeb project that can be extended with your implementation and initialized with a `CredentialStore`.

>Note: The example server uses ASP.Net Core Mvc. However, you can use any web framework to implement your endpoint using the same pattern shown and objects provided in this repository.

## Project Overview

### DTASDK

DTASDK contains objects that help with verifying the request signature and serializing the request payload to a DTA message object.

#### Signature Verification

Messages sent to an AIA endpoint are sign using a modified [AWSv4 signing process]. The `Signer` class can verify the signature used to sign the request. This ensures the request was sent by Amazon. The `IDTARequest` interface it uses gets the required content from the request.

### DTASDKTests

Test for the signing and serialization logic. Of note are the `SignerTests.roundTrip` tests, which can be used to test how a request would be signed and verified by setting your own test request headers and body.

### DTASDKWeb

An ASP.Net Core project containing abstract controllers, `AccountLinkingController` and `PurchaseController`, that handle verifying the request signature and serializing the `InstantAccessRequest` POJOs. `DTARequest` is an implementation of `IDTARequest` wrapping an ASP.Net `HttpRequest`. Extending the controllers lets you directly implement your account linking or purchase fulfillment logic.

Example account linking controller:

    using System.Threading.Tasks;
    using Amazon.DTASDK.V2;
    using Amazon.DTASDK.V2.Signature;
    using DTASDKWeb.Controllers;
    using Microsoft.AspNetCore.Mvc;

    namespace Vendor.AIA.Controllers
    {
        [Route("aia/accountLinking")]
        public class VendorAccountLinkingController : AccountLinkingController
        {
            public VendorAccountLinkingController() {
                CredentialStore = new CredentialStore();
                CredentialStore.LoadFromContents("SECRETKEY PUBKEY");
            }

            protected override Task<GetUserIdResponse> GetUserId(GetUserIdRequest request)
            {
                return Task.FromResult(new GetUserIdResponse() {
                    UserId = "USERID"
                });
            }
        }
    }

## Contact Us

If you have any questions about AIA, please email us at d3-support@amazon.com.

Happy Coding! ❤️

[Paket]: <https://fsprojects.github.io/Paket/index.html>
[AWSv4 signing process]: <https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html>
[Java Sdk]: <https://s3-us-west-2.amazonaws.com/dtg-docs/java/index.html>