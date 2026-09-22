# RiseOn.Utils.Network

Tiện ích mạng: lấy giờ hiện tại từ mạng, không phụ thuộc đồng hồ của máy, và
kiểm tra máy có vào được internet thật hay không.

Package `com.riseon.utils.network`, namespace `RiseOn.Utils.Network`. Không phụ
thuộc package nào khác.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |

Không cần Odin, DOTween hay package RiseOn nào khác.

Không hỗ trợ WebGL: trình duyệt chặn UDP (NTP) và không cho đọc response của
domain khác. Gọi trên WebGL nhận về task lỗi `PlatformNotSupportedException`.

Android cần quyền `INTERNET`. Project có SDK quảng cáo hay Firebase thì đã có
sẵn; nếu không, đặt *Internet Access* = *Require* trong Player Settings.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.network": "1.0.0"
  }
}
```

**Git URL**:

```
https://github.com/riseongamestudio/Utils.git?path=/Network#com.riseon.utils.network/1.0.0
```

**Thư mục local**: `"com.riseon.utils.network": "file:D:/path/to/Utils/Network"`.

## Hướng dẫn nhanh

**Lấy giờ từ mạng**, cho những chỗ người chơi có thể chỉnh giờ máy để gian lận
(quà theo ngày, thời gian hồi, sự kiện có hạn):

```csharp
using RiseOn.Utils.Network;

try {
    DateTime utcNow = await NetworkTime.NowAsync();
} catch (TimeoutException) {
    // Không nguồn nào trả lời trong 2 giây
}
```

**Kiểm tra internet**:

```csharp
if (!Connectivity.HasNetwork) {
    // Máy không có kết nối nào, khỏi gửi request
}

bool online = await Connectivity.HasInternetAsync();
```

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| `NetworkTime` | Giờ UTC từ NTP, dự phòng bằng header `Date` của HTTPS | [Runtime/NetworkTime](Runtime/NetworkTime/README.md) |
| `Connectivity` | Máy có mạng không, vào được internet không | [Runtime/Connectivity](Runtime/Connectivity/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
