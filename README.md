# SmartBin

스마트 쓰레기통 관리 시스템 - ASP.NET Blazor 기반 웹 애플리케이션
쓰레기통(Bin) 상태 모니터링, 수거 트럭(Truck) 관리, 알림(Alert), Unity WebGL 시뮬레이션 임베드 기능을 제공합니다.

## 기술 스택

- **프레임워크**: ASP.NET Core 8.0 (Blazor Web App, Interactive Server)
- **언어**: C#
- **UI**: Blazor Components
- **데이터베이스**: Supabase (PostgREST 클라이언트 `supabase-csharp`)
- **시뮬레이션**: Unity WebGL 빌드 임베드 (`wwwroot/webgl/`)

## 주요 기능

- 대시보드(Home): 전체 쓰레기통/트럭 통계 요약
- 쓰레기통 목록 / 상세: 적재율·좌표·상태, 상세 페이지에서 Unity WebGL 시뮬레이션 자동 포커싱
- 수거 트럭 목록 / 상세
- 알림 페이지

## 사전 요구사항

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 이상
- Supabase 프로젝트 (URL, Anon Key)

## 설정

`appsettings.json` 또는 사용자 시크릿에 Supabase 접속 정보가 필요합니다.

```json
{
  "Supabase": {
    "Url": "https://<your-project>.supabase.co",
    "AnonKey": "<your-anon-key>"
  }
}
```

> **주의**: 커밋된 키는 anon(public) 키여야 하며 `service_role` 키는 절대 클라이언트/저장소에 두지 마세요.
> 운영 환경에서는 환경변수 `Supabase__Url`, `Supabase__AnonKey` 또는 사용자 시크릿 사용을 권장합니다.

### Supabase 측 체크리스트

- `trash_bin_state_latest`, `trash_truck_state_latest` 등 모델이 참조하는 **테이블/뷰가 존재**해야 합니다.
- 각 테이블의 **RLS(Row Level Security) 정책**이 anon 키에 대해 `SELECT` 를 허용해야 합니다(정책이 없으면 빈 결과가 반환됨).

## 실행 방법

```bash
# 저장소 클론
git clone https://github.com/2026-CS-team-project/smartbin-web.git
cd smartbin-web

# 패키지 복원 및 실행
dotnet run
```

실행 후 터미널에 표시되는 URL(예: `https://localhost:7xxx`)로 브라우저에서 접속합니다.

## 프로젝트 구조

```
smartbin-web/
├── Components/
│   ├── Pages/                # 라우팅 페이지
│   │   ├── Home.razor        # 대시보드
│   │   ├── Bins.razor        # 쓰레기통 목록
│   │   ├── BinDetail.razor   # 쓰레기통 상세 + WebGL 임베드
│   │   ├── Trucks.razor      # 수거 트럭 목록
│   │   ├── TruckDetail.razor # 수거 트럭 상세
│   │   └── Alerts.razor      # 알림
│   └── ...
├── Models/                   # Postgrest BaseModel 상속 데이터 모델
│   ├── Bin.cs
│   ├── Truck.cs
│   └── Alert.cs
├── Services/                 # Supabase 쿼리 래퍼
│   ├── BinService.cs
│   ├── TruckService.cs
│   └── AlertService.cs
├── wwwroot/
│   ├── app.css
│   └── webgl/                # Unity WebGL 빌드 (index.html, Build/, TemplateData/)
├── Program.cs                # 진입점 + Supabase/서비스 DI 등록
├── appsettings.json          # 설정 파일
└── SmartBin.csproj
```

## Unity WebGL 시뮬레이션 연동

`wwwroot/webgl/index.html` 은 정적 파일로 서빙되며, 쓰레기통 상세 페이지(`/bins/{id}`)에서 `<iframe>` 으로 임베드됩니다.

- 임베드 URL: `/webgl/index.html?bin=<번호>`
- 쿼리 `?bin=<int>` 가 전달되면 Unity 인스턴스 로드 후 자동으로
  `SendMessage("TrashCan Camera Navigator", "MoveToTrashCanNumber", <int>)` 를 호출하여 해당 쓰레기통으로 카메라를 이동시킵니다.
- 수동으로 시뮬레이션 우상단 `No.` 입력 + `Go` 버튼을 사용해도 동일하게 동작합니다.

`.gz` 정적 파일 서빙을 위해 `Program.cs` 에서 `FileExtensionContentTypeProvider` 와 `Content-Encoding: gzip` 헤더를 설정합니다.

## 새 테이블/엔티티 추가 시 절차

1. `Models/Xxx.cs` 에 `BaseModel` 상속 + `[Table]`, `[Column]`, `[PrimaryKey]` 어트리뷰트 작성
2. `Services/XxxService.cs` 에 쿼리 메서드 작성 (생성자 주입으로 `Supabase.Client` 수신)
3. `Program.cs` 에 `builder.Services.AddScoped<XxxService>();` 등록
4. Razor 페이지에서 `@inject SmartBin.Services.XxxService XxxService` 로 사용
5. Supabase 콘솔에서 해당 테이블의 RLS 정책 확인

## 라이선스

MIT
