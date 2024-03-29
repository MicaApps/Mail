<p align="center">
    <img src="https://user-images.githubusercontent.com/6630660/217154573-9489676a-b34b-4523-aba4-05cd9ed81f97.png" alter="Mail Icon" align="center"/>
    <h1 align="center">Mail</h1>
    <p align="center">An Email client, reimagined in the WinUI style.</p>
</p>

## Download

<a style="margin-left:24px" href="https://www.microsoft.com/store/productId/9NVMM1QDW3QB">
    <picture>
        <source media="(prefers-color-scheme: dark)" srcset="https://get.microsoft.com/images/en-us%20light.svg" />
        <source media="(prefers-color-scheme: light)" srcset="https://get.microsoft.com/images/en-us%20dark.svg" />
        <img style="vertical-align:middle" src="https://get.microsoft.com/images/en-us%20dark.svg" alt="Download DiskInfo" />
    </picture>
</a>

## Objective

We're striving to create a UWP Mail client for Windows.

![image](https://github.com/MicaApps/Mail/assets/6630660/9980ef06-43f8-4708-bcd0-195717798361)


## Roadmap

Discover our [upcoming features](Roadmap.md).

## Build

1. Clone the repository.
2. Add a `Secret.cs` file in the `build` folder (see below).
3. Open the Mail.sln file with Visual Studio.
4. Add 
```
https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json 
```
the link to Nuget source

More info

https://github.com/CommunityToolkit/Labs-Windows

5. Compile the solution.

In the `build` folder, generate a `Secret.cs` file and embed the following code:

```csharp
namespace Mail
{
    internal class Secret
    {
        // Replace this with your unique AAD ClientId from Azure Active Directory.
        // Azure Active Directory: https://github.com/DiskTools/Mail/tree/GraphAuth
        public static readonly string AadClientId = "Your AAD ClientId";
    }
}
```

Alternatively, you can find an example in our development-oriented chat group, such as a QQ group.

## Contribution

We encourage creating your own branch instead of directly developing and committing to the main branch. Once your code is polished and ready, submit a Pull Request. Your contribution will be merged into the main branch only after it has undergone a Code Review by other members of the team.
