# Connectivity

[← RiseOn.Utils.Network](../../README.md)

Kiểm tra máy có mạng không, và có vào được internet thật hay không.

## Cách dùng

```csharp
using RiseOn.Utils.Network;

if (!Connectivity.IsNetworkReachable) {
    // Máy không có kết nối nào: bỏ qua request, báo offline
}

bool online = await Connectivity.HasNetworkAsync();
```

| API | Trả về | Chi phí |
|---|---|---|
| `IsNetworkReachable` | `true` khi máy có đường ra mạng: wifi, cáp hoặc dữ liệu di động đang nối. Không biết bên kia có trả lời không, nên wifi mất internet vẫn là `true` | Tức thì, không gửi request. Chỉ gọi trên main thread |
| `HasNetworkAsync(timeoutSeconds = 2, cancellationToken)` | `true` khi internet trả lời thật: một endpoint kiểm tra trả về 204 (WebGL: trả lời bất kỳ) | Gửi song song 3 request HTTPS nhỏ, xong ngay khi có cái đầu tiên thành công |

## HasNetworkAsync hoạt động thế nào

Gửi song song tới `connectivitycheck.gstatic.com/generate_204`,
`www.google.com/generate_204` và `cp.cloudflare.com/generate_204`, những
endpoint Android, Chrome và Cloudflare dùng để kiểm tra kết nối. Cái nào trả về
204 trước thì trả `true` và hủy các request còn lại.

- Wifi bắt đăng nhập (khách sạn, quán cà phê) trả về trang đăng nhập thay vì 204,
  nên được tính là không có internet.
- Hết `timeoutSeconds` mà chưa có 204 thì trả `false`, không ném lỗi.
- Chỉ ném `OperationCanceledException` khi `cancellationToken` bị hủy.
- Trên WebGL, trình duyệt không cho đọc response của `generate_204`, nên thay bằng
  `www.cloudflare.com/cdn-cgi/trace`, `1.1.1.1/cdn-cgi/trace` và `timeapi.io`, là
  những endpoint cho phép đọc (CORS). Có trả lời là tính có internet; phải gọi từ
  main thread.
