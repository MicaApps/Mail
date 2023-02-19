#  ![Mail2@2x](https://user-images.githubusercontent.com/6630660/217154573-9489676a-b34b-4523-aba4-05cd9ed81f97.png) Mail

Aim at providing UWP Mail for Windows.

![image](https://user-images.githubusercontent.com/6630660/216893484-808cb5ed-4726-42d2-82e0-ac35c53fb7b3.png)

## Technology
1. Azure Active Directory

https://github.com/DiskTools/Mail/tree/GraphAuth

2. RichTextToolbar

- https://www.syncfusion.com/uwp-ui-controls/richtextbox

- Windows Community Toolkit/Controls/TextToolbar


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

## Contribution
You should not develop and commit code directly based on the `main` branch, but instead create your own branch, submit a `Pull Request` after the code is completed, and merge into the `main` branch after `Code Review` by others.


