# Phát hành cập nhật NNPA Media

Thông tin VPS thật và URL công khai không được lưu trong repository. Sao chép `deployment/vps-deploy-config.example.json` thành `deployment/vps-deploy-config.local.json`, sau đó điền cấu hình triển khai thật. Tệp `.local.json` đã được Git bỏ qua.

## Tệp cần phát hành

1. `NNPA-Media-Setup-x.y.z.exe`
2. `version.json`

Luôn tải bộ cài lên trước và `version.json` sau cùng để máy khách không nhìn thấy bản phát hành chưa hoàn chỉnh. Manifest được tạo trong `artifacts/release` và chứa URL tải cùng SHA-256 tương ứng. Không lưu mật khẩu VPS trong repository; dùng SSH key hoặc cơ chế xác thực của OpenSSH.

Tạo bản phát hành local:

```powershell
.\release.ps1 -Version 1.0.0
```

Tạo và tải bản phát hành lên VPS:

```powershell
.\release.ps1 -Version 1.0.0 -Upload
```

Các artifact được tạo trong `artifacts/release` và không được Git theo dõi.
