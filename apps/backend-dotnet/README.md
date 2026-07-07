# Backend .NET Skeleton

Đây là thư mục dành cho ASP.NET Core backend.

Khuyến nghị tạo solution:

```bash
dotnet new sln -n RemoteAssignment
mkdir src tests
cd src
dotnet new webapi -n RemoteAssignment.WebApi
dotnet new classlib -n RemoteAssignment.Domain
dotnet new classlib -n RemoteAssignment.Application
dotnet new classlib -n RemoteAssignment.Infrastructure
dotnet new worker -n RemoteAssignment.Worker
```

Sau đó add reference theo rules trong `rules/backend-dotnet-rules.md`.
