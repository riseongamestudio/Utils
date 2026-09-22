# Connectivity

[← RiseOn.Utils.Network](../../README.md)

Kiểm tra máy có mạng không, và có vào được internet thật hay không.

## Cách dùng

```csharp
using RiseOn.Utils.Network;

if (!Connectivity.HasNetwork) {
    // Máy không có kết nối nào: bỏ qua request, báo offline
}

bool online = await Connectivity.HasInternetAsync();
```

| API | Trả về | Chi phí |
|---|---|---|
| `HasNetwork` | `false` khi máy không có cả wifi lẫn dữ liệu di động. `true` chưa chắc đã vào được internet | Tức thì, không gửi request. Chỉ gọi trên main thread |
| `HasInternetAsync(timeoutSeconds = 2, cancellationToken)` | `true` khi một endpoint kiểm tra trả về 204 | Gửi song song 3 request HTTPS nhỏ, xong ngay khi có cái đầu tiên thành công |

## HasInternetAsync hoạt động thế nào

Gửi song song tới `connectivitycheck.gstatic.com/generate_204`,
`www.google.com/generate_204` và `cp.cloudflare.com/generate_204`, những
endpoint Android, Chrome và Cloudflare dùng để kiểm tra kết nối. Cái nào trả về
204 trước thì trả `true` và hủy các request còn lại.

- Wifi bắt đăng nhập (khách sạn, quán cà phê) trả về trang đăng nhập thay vì 204,
  nên được tính là không có internet.
- Hết `timeoutSeconds` mà chưa có 204 thì trả `false`, không ném lỗi.
- Chỉ ném `OperationCanceledException` khi `cancellationToken` bị hủy.
- WebGL không hỗ trợ: gọi vào nhận về task lỗi `PlatformNotSupportedException`.
