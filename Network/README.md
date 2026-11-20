# RiseOn.Utils.Network

Tiện ích mạng: lấy giờ hiện tại từ mạng, không phụ thuộc đồng hồ của máy, và
kiểm tra máy có vào được internet thật hay không.

Package `com.riseon.utils.network`, namespace `RiseOn.Utils.Network`. Ngoài
Odin, không phụ thuộc package nào khác, chỉ dùng module có sẵn của Unity.

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
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Pack không dùng Odin, nhưng mọi pack RiseOn đều cần nó, để cài đặt như nhau |
| `com.unity.modules.unitywebrequest` 1.0.0 | Tự cài theo `package.json`, có sẵn trong Unity | Gửi request qua trình duyệt trên WebGL |

Odin không có trên UPM nên phải cài vào project trước. Thiếu Odin thì project báo một
lỗi từ `RiseOn.Utils.Network.Requirements`.

Trên WebGL, trình duyệt không có UDP (NTP) và chỉ cho đọc response của domain
cho phép (CORS), nên cả hai hàm đi qua HTTPS tới các endpoint cho phép đọc. Trên
WebGL phải gọi từ main thread.

Android cần quyền `INTERNET`. Project có SDK quảng cáo hay Firebase thì đã có
sẵn; nếu không, đặt *Internet Access* = *Require* trong Player Settings.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.network": "2.0.1"
  }
}
```

**Git URL**: *Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/Network
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
if (!Connectivity.IsNetworkReachable) {
    // Máy không có kết nối nào, khỏi gửi request
}

bool online = await Connectivity.HasNetworkAsync();
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
