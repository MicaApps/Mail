#  ![Mail2@2x](https://user-images.githubusercontent.com/6630660/217154573-9489676a-b34b-4523-aba4-05cd9ed81f97.png) Mail 开发参考

## Microsoft Graph API 与 MSAL 参考及注意事项
- DEMO 就是这个分支里面的 `WpfGraphAPITest-Mail` 项目
- 这种身份验证叫做“代表用户获取访问权限”
- DEMO 只能在 Windows 下使用

![image.png](https://s2.loli.net/2023/02/08/lJL6eaDpkyrM1ij.png)

## Postman 调试相关
### 首次认证授权
1. 打开 https://www.postman.com/microsoftgraph/workspace/microsoft-graph/collection/455214-085f7047-1bec-4570-9ed0-3a7253be148 ，登录

2. 创建一个 Fork

![image.png](https://s2.loli.net/2023/02/08/4z96BkRKWyZlIvp.png)

3. 新建一个 `GetToken` POST 请求

URL：
```
https://login.microsoftonline.com/consumers/oauth2/v2.0/token
```

Body（x-www-form-urlencoded）：
|key|value|
|---|---|
|client_id|0b3dac55-dc21-442b-ace7-ccefbb5a9f80|
|scope|Mail.ReadWrite offline_access|
|code|M.R3_BAY.xxxxxxxxxxxxxxx|
|redirect_uri|https://login.microsoftonline.com/common/oauth2/nativeclient|
|grant_type|authorization_code|

4. 浏览器里打开 `https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=0b3dac55-dc21-442b-ace7-ccefbb5a9f80&response_type=code&redirect_uri=https%3A%2F%2Flogin%2Emicrosoftonline%2Ecom%2Fcommon%2Foauth2%2Fnativeclient&response_mode=query&scope=Mail.ReadWrite%20User.Read%20offline_access&state=12345`，根据提示登录授权

5. 完成后跳转到 `https://login.microsoftonline.com/common/oauth2/nativeclient?code=M.R3_BAY.xxxxxxxxxxx&state=12345`，复制 code 部分（不要把 `&state=12345` 也复制进去了！！！！！）

6. 粘贴到 Postman 那个请求的 Body 的 code 处

7. 发送请求，拿到 `access_token` 和 `refresh_token`

### 访问 Graph API
1. 点击 `Delegated`

![image.png](https://s2.loli.net/2023/02/08/7nrZEmqsSDOcpi5.png)

2. 在右侧把刚才拿到的 `access_token` 粘贴到这里，`Ctrl+S` 保存

![image.png](https://s2.loli.net/2023/02/08/G6YSCgj3OPWeLkh.png)

3. 打开一个 Mail 请求

![image.png](https://s2.loli.net/2023/02/08/RGZ5MYAvnQSEabJ.png)

4. 点击发送，可以看到返回了数据

![image.png](https://s2.loli.net/2023/02/08/TNxUcnvRJm8LgZu.png)

### 使用 `refresh_token` 刷新 `access_token`
1. 新建一个 `Refresh` POST 请求

URL：
```
https://login.microsoftonline.com/consumers/oauth2/v2.0/token
```

Body（x-www-form-urlencoded）：
|key|value|
|---|---|
|client_id|0b3dac55-dc21-442b-ace7-ccefbb5a9f80|
|scope|Mail.ReadWrite offline_access|
|refresh_token|M.R3_BAY.xxxxxxxxxxxxxxx|
|grant_type|refresh_token|

2. 把刚才获得的 `refresh_token` 粘贴到表单对应位置上

3. 发送请求，获取 `access_token` 和新的 `refresh_token`