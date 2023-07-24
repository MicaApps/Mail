<p align="center">
    <img src="https://user-images.githubusercontent.com/6630660/217154573-9489676a-b34b-4523-aba4-05cd9ed81f97.png" alter="Mail Icon" align="center"/>
    <h1 align="center">Mail</h1>
    <p align="center">E-Mail client with WinUI style redeisgn.</p>
</p>

## Download
<a style="margin-left:24px" href="https://www.microsoft.com/store/productId/9NVMM1QDW3QB">
    <picture>
        <source media="(prefers-color-scheme: dark)" srcset="https://get.microsoft.com/images/en-us%20light.svg" />
        <source media="(prefers-color-scheme: light)" srcset="https://get.microsoft.com/images/en-us%20dark.svg" />
        <img style="vertical-align:middle" src="https://get.microsoft.com/images/en-us%20dark.svg" alt="Download DiskInfo" />
    </picture>
</a>

## Target

Aim at providing UWP Mail for Windows.

![Concept](https://user-images.githubusercontent.com/6630660/222345692-16ca601a-9e86-4d81-a3f4-3c4773e31b88.png)

## Roadmap

the [latest features](Roadmap.md) that we planned.

## O-Auth
- [Azure Active Directory](https://github.com/DiskTools/Mail/tree/GraphAuth)

## Build

### NuGet Config

Use the `nuget.config` file in the root of the repository to configure the NuGet sources for the solution.

or you should add the following nuget source:

* `https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json`

### Add `Secret.cs` 

Create `Secret.cs` file in `build` folder, and add the following code:

```
namespace Mail
{
    internal class Secret
    {
        // You should replace it with your own AAD ClientId in Azure Active Directory.
        public static readonly string AadClientId = "Your AAD ClientId";
    }
}

```

or you can find one in group file in development group.


## Contribution

You should not develop and commit code directly based on the `main` branch, but instead create your own branch, submit a `Pull Request` after the code is completed, and merge into the `main` branch after `Code Review` by others.
