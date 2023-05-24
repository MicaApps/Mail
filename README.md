<p align="center">
    <img src="https://user-images.githubusercontent.com/6630660/217154573-9489676a-b34b-4523-aba4-05cd9ed81f97.png" alter="Mail Icon" align="center"/>
    <h1 align="center">Mail</h1>
    <p align="center">E-Mail client with WinUI style redeisgn.</p>
</p>

## Target

Aim at providing UWP Mail for Windows.

![Concept](https://user-images.githubusercontent.com/6630660/222345692-16ca601a-9e86-4d81-a3f4-3c4773e31b88.png)

## O-Auth
- [Azure Active Directory](https://github.com/DiskTools/Mail/tree/GraphAuth)

## Build
### 1. Add `CommunityToolkitLab` nuget source guide , see :
https://github.com/CommunityToolkit/Labs-Windows

### 2. Create `Secret.cs` in the `Mail` folder, the source code like this
```
namespace Mail
{
    internal class Secrect
    {
        public static readonly string AadClientId = "Your AAD ClientId";
    }
}

```
### 3. Warming pfx not matching
https://www.cnblogs.com/kljzndx/p/14381823.html

## Contribution
You should not develop and commit code directly based on the `main` branch, but instead create your own branch, submit a `Pull Request` after the code is completed, and merge into the `main` branch after `Code Review` by others.


