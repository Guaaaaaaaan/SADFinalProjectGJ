<h1 align="center">IGPTS – Invoice Generation and Payment Tracking System</h1>

<p align="center">
  <b> Bilingual Documentation | 中英双语 </b><br>
   Click the buttons below to switch languages · 点击下方按钮切换语言 
</p>

---
## **English Version (Click to Expand)**
<details>
<summary><b>Expand / Collapse English Content</b></summary>

<br>

# IGPTS – Invoice Generation and Payment Tracking System

IGPTS is an enterprise-grade web application designed for small and medium-sized businesses to manage invoices, track Stripe payments, and generate real-time financial analytics.  
Built on **ASP.NET Core MVC**, it includes automation services, audit logs, dual-layer security validation, and a strict invoice lifecycle workflow.

---

## 🚀 Key Features

### **Invoice Lifecycle Management**
- Draft → Sent → Paid / Overdue
- Manual “Send Invoice” trigger
- Sent invoices are backend-locked from edits
- Soft-delete archiving for clean record management

### **Security & Compliance**
- Role-Based Access Control: Admin / FinanceStaff / Client
- Dual-layer GST tampering protection  
  - Frontend: read-only for non-admin users  
  - Backend: ignores unauthorized modifications
- Immutable audit log (Create/Edit/Send/Delete)

### **Analytics & Reporting**
- Chart.js interactive dashboard  
- Aging report (30 / 60 / 60+ days)  
- Excel data export via ClosedXML

### **Automation**
- Background service automatically marks overdue invoices  
- Database auto-creation and demo data seeding

---

## 🛠️ Technology Stack

| Category | Technologies |
|---------|--------------|
| Framework | ASP.NET Core 6/7 MVC |
| Database | SQL Server, EF Core |
| Authentication | Identity |
| Payment | Stripe API |
| Frontend | Bootstrap 5, jQuery, Chart.js |
| Tools | ClosedXML, MailKit |

---

## 🏁 Getting Started

### Prerequisites  
- Visual Studio 2022  
- SQL Server  
- .NET SDK  

### Installation Steps  
1. Clone the repository  
2. Open `SADFinalProjectGJ.sln`  
3. Modify connection string if needed  
4. Press F5 to run  
   - DB auto-seeding enabled  

---

## 🔐 Default Login Credentials

| Role | Username | Email | Password |
|------|----------|--------|----------|
| Admin | admin | admin@igpts.com | Password123! |
| Staff | staff | staff@igpts.com | Password123! |
| Client | demo_client | client@igpts.com | Password123! |

---

## 🧪 GST Logic-Lock Security Test

1. Log in as Staff  
2. Create Invoice  
3. GST field is read-only  
4. Remove `readonly` via Developer Tools  
5. Submit  

### ✔ Expected Result  
- Backend ignores tampered GST  
- Uses global Admin-defined GST rate  
- Security validation confirmed  

</details>

---

## **中文说明（点击展开）**
<details>
<summary><b>展开 / 收起中文内容</b></summary>

<br>

# IGPTS – 发票生成与支付追踪系统

IGPTS 是一款企业级 Web 应用程序，专为中小型企业设计，用于简化财务操作。系统基于 **ASP.NET Core MVC** 构建，可安全地创建发票、追踪 Stripe 支付，并生成实时财务分析数据。

本项目包含自动化服务、审计日志、双层参数保护、严格的发票生命周期等高级功能。

---

## 🚀 主要功能

### **发票生命周期管理**
- 草稿 → 已发送 → 已支付 / 逾期
- 需手动点击“发送发票”
- 发送后的发票将被后端锁定，无法编辑
- 支持软删除归档

### **安全与合规**
- 基于角色的访问控制（Admin / FinanceStaff / Client）
- GST 税率双层防篡改  
  - 前端：非管理员为只读  
  - 后端：忽略非管理员篡改的值
- 不可修改的审计日志（创建/编辑/发送/删除）

### **数据分析与报表**
- Dashboard（Chart.js）  
- 账龄分析（30 / 60 / 60+ 天）  
- Excel 导出（ClosedXML）

### **自动化功能**
- 背景服务自动标记逾期发票
- 系统自动创建数据库与初始化数据

---

## 🛠️ 技术栈

| 分类 | 技术 |
|------|------|
| 框架 | ASP.NET Core 6/7 MVC |
| 数据库 | SQL Server, EF Core |
| 身份验证 | Identity |
| 支付 | Stripe API |
| 前端 | Bootstrap 5, jQuery, Chart.js |
| 工具 | ClosedXML, MailKit |

---

## 🏁 入门指南

### 环境需求  
- Visual Studio 2022  
- SQL Server  
- .NET SDK  

### 安装步骤  
1. 克隆仓库  
2. 打开 `SADFinalProjectGJ.sln`  
3. 修改连接字符串（如需要）  
4. F5 运行即可  
   - 数据库会自动创建

---

## 🔐 默认账号

| 角色 | 用户名 | 邮箱 | 密码 |
|------|--------|--------|--------|
| Admin | admin | admin@igpts.com | Password123! |
| Staff | staff | staff@igpts.com | Password123! |
| Client | demo_client | client@igpts.com | Password123! |

---

## 🧪 GST 参数安全测试

1. 使用 Staff 登录  
2. 创建发票  
3. GST 字段为只读  
4. F12 移除 readonly 并修改  
5. 提交  

### ✔ 预期结果  
- 后端忽略被篡改值  
- 使用系统默认 GST  
- 双层保护生效  

</details>

---