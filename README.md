# **🚀 RESTful API: .NET 8 + SQL Server 2022 (Docker)**
這是一個基於 **ASP.NET Core** 的 RESTful API，使用 **SQL Server 2022** 作為後端資料庫，並透過 **Docker Compose** 進行容器化部署。

## 🌟 專案特色
✅ **基於 ASP.NET Core 8**，提供 RESTful API 支援 `CRUD（建立 / 讀取 / 更新 / 刪除）`    
✅ **使用 JWT 身份驗證**，此專案示範驗證保護 `/users/{id} 的 PUT 與 DELETE 2個api端點` 
✅ **容器化部署**（Docker Compose 一鍵啟動 `.NET 8 API + SQL Server` ）  
✅ **SQL Server 2022 整合**，支援完整的資料庫操作  
✅ **Adminer** 作為 Web UI 管理資料庫工具，簡單易用(本機如有安裝SSMS，亦可連線 127.0.0.1 管理資料庫，帳密請參考api/appsettings.json)  
✅ **支援 API 測試**（Postman、cURL、PowerShell）  
✅ **提供 HTML 頁面呼叫使用 API**（使用者列表:user-list.html, 新增:user-add.html, 編輯:user-edit.html）  
✅ **提供 Swagger UI** 提供直觀的 API 測試介面 (使用 Dockerfile 安裝 Swashbuckle.AspNetCore，確保 Swagger 可用）  
✅ **熱重載**：開發時支援 `dotnet watch run` ，程式碼異動會 hot-reload

---

## 📦 環境需求
✔ **Docker Desktop**（用於容器化 `.NET API` 和 `SQL Server`）  
✔ **.NET 8 SDK**（Docker Compose 啟動自動下載 mcr.microsoft.com/dotnet/sdk:8.0 與容器化部署.NET SDK）  
✔ **Postman**（可選，亦可使用 cURL，測試 REST API）

---

## **🔹 目錄結構**
```
.
├── README.md              # 專案說明文件
├── docker-compose.yaml    # Docker 設定檔
├── init-db.sql            # SQL 資料庫初始化
├── api/                   # .NET API 原始碼
│   ├── Dockerfile         # .NET API 的 Dockerfile（安裝 Swagger）
│   ├── appsettings.json   # 資料庫連線帳密參數設定
│   ├── api.csproj         # .NET 項目設定
│   ├── Program.cs         # 主 API 程式
│   ├── services/          # 子目錄:服務類 程式 
│   	  ├── DatabaseService.cs # 資料庫 服務
│       ├── JwtService.cs      # JWT驗證 服務
├── www/                   # html頁面(呼叫 users 相關REST-API功能測試)
│   ├── user-list.html     # 使用者 列表 html頁面
│   ├── user-add.html      # 使用者 新增 頁面
│   ├── user-edit.html     # 使用者 修改 頁面

```

---

## **🛠 安裝與運行**
### **1️⃣ 下載專案**
```sh
git clone https://github.com/cchhss123/dotnet_restapi.git
cd dotnet_restapi
```

### **2️⃣ 啟動 Docker**
使用 `docker-compose` 來啟動 SQL Server 2022 和 `.NET API`：
```sh
docker-compose up -d
```
✅ **成功啟動後：**
- `.NET API` 在 `http://localhost:3000`
- `API Swagger UI` 在 `http://localhost:3000/swagger`
- `Adminer` 在 `http://localhost:8080`

### **3️⃣ 建立資料庫**
使用 `Adminer` 匯入 `init-db.sql` 來建立 **`demo` 資料庫** 和 `users` 表：

1️⃣ **開啟 Adminer** 👉 `http://localhost:8080`  
2️⃣ **登入資料庫**
   - **系統**: `MS SQL`
   - **伺服器**: `sqlsrv2022`
   - **使用者**: `sa`
   - **密碼**: `MSsql2022`
   - **資料庫**: 
     
3️⃣ **點選[匯入]功能，選擇 `init-db.sql`檔案後，執行匯入**（建立`demo` 資料庫 與 `users` 表）

PS: 本機如有安裝SSMS，亦可連線 127.0.0.1 管理資料庫，使用 SSMS 建立`demo` 資料庫 與 `users` 表
---

## 🖥 API 測試（RESTful API）

使用 Postman、cURL 或 PowerShell 測試 API，或使用 Swagger UI (http://localhost:3000/swagger)。

### 📌 API 端點：
|  方法  |  路徑                           | 描述          | 認證要求          |
|--------|--------------------------------|---------------|---------------|
| POST   | `/auth`                       | 認證API帳號，取得 JWT Token | ❌ 不需要 |
| GET    | `/users`                       | 取得所有使用者  | ❌ 不需要 |
| GET    | `/users/{id}`                  | 取得特定使用者  | ❌ 不需要 |
| POST   | `/users`                       | 新增使用者      | ❌ 不需要 |
| PUT    | `/users/{id}`                  | 更新使用者資料  | ✅ 需要 JWT |
| DELETE | `/users/{id}`                  | 刪除使用者    | ✅ 需要 JWT |


📌 PUT 和 DELETE 端點 需要 JWT Token，請先執行 /auth 來取得 Token。

### **📌 取得 JWT Token**
```sh
curl -X POST http://localhost:3000/auth -H "Content-Type: application/json" -d '{"Account": "api", "Password": "test"}'
```
如果是 WINDOWS PowerShell 使用以下指令
```sh
$headers = @{ "Content-Type" = "application/json" }
Invoke-WebRequest -Uri "http://localhost:3000/auth" -Method POST -Headers $headers -Body '{"Account": "api", "Password": "test"}'
```

成功回應範例：

```json
{
  "message": "Login successful",
  "token": "your.jwt.token"
}
```
📌 請記住 your.jwt.token，後續 API 需要使用！


### **📌 取得所有使用者**
```sh
curl -X GET http://localhost:3000/users
```
如果是 WINDOWS PowerShell 使用以下指令
```sh
Invoke-WebRequest -Uri "http://localhost:3000/users" -Method GET
```

### **📌 取得特定id使用者(例如:id=1)**
```sh
curl -X GET http://localhost:3000/users/1
```
如果是 WINDOWS PowerShell 使用以下指令
```sh
Invoke-WebRequest -Uri "http://localhost:3000/users/1" -Method GET
```

### **📌 新增使用者**
```sh
curl -X POST http://localhost:3000/users -H "Content-Type: application/json" -d '{"name": "andrew","email": "cchhss123@hotmail.com"}'
```
如果是 WINDOWS PowerShell 使用以下指令
```sh
$headers = @{ "Content-Type" = "application/json" }
Invoke-WebRequest -Uri "http://localhost:3000/users" -Method POST -Headers $headers -Body '{"name": "andrew","email": "cchhss123@hotmail.com"}'
```

### **📌 更新使用者（需要 JWT Token）**
```sh
curl -X PUT http://localhost:3000/users/1 -H "Authorization: Bearer your.jwt.token" -H "Content-Type: application/json" -d '{"name": "andy", "email": "andy@example.com"}'
```
如果是 WINDOWS PowerShell 使用以下指令
```sh
$headers = @{ "Authorization" = "Bearer your.jwt.token"; "Content-Type" = "application/json" }
Invoke-WebRequest -Uri "http://localhost:3000/users/1" -Method PUT -Headers $headers -Body '{"name": "andy", "email": "andy@example.com"}'
```

### **📌 刪除使用者（需要 JWT Token）**
```sh
curl -X DELETE http://localhost:3000/users/6 -H "Authorization: Bearer your.jwt.token"
```
如果是 WINDOWS PowerShell 使用以下指令
```sh
$headers = @{ "Authorization" = "Bearer your.jwt.token" }
Invoke-WebRequest -Uri "http://localhost:3000/users/1" -Method DELETE -Headers $headers
```

---

## **使用html頁面呼叫使用API(使用者列表/使用者 新增 修改 刪除)**

瀏覽器 開啟 www/user-list.html 檔案，可進行相關API呼叫使用與測試，
包含: 使用者列表(含 使用者 編輯&刪除功能)，使用者新增 等功能。

---

## **🛠 docker容器 停止與刪除**
停止並刪除所有容器：
```sh
docker-compose down
```





