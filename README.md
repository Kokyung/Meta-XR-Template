# Meta XR Template

Meta Quest 3/3S용 VR·MR 프로젝트를 시작하기 위한 Unity 템플릿입니다. 패스스루와 핸드 트래킹 샘플, 공용 개발 코드를 포함합니다.

## 개발 환경

- Unity **6000.6.0f1**
- Meta XR All-in-One SDK **207.0.1**
- URP **17.6.0**, OpenXR **1.18.0**
- Android 빌드 시 Android Build Support 및 SDK·NDK·OpenJDK 필요

## 포함 내용

- `Assets/Scenes/PassthroughSample.unity`: 카메라 리그, 패스스루, 핸드 트래킹, 잡기·Poke 상호작용 및 공간 오디오 샘플
- Meta XR Interaction SDK 예제 씬
- Meta Quest용 Android 빌드 프로필 및 PC·Meta Quest용 URP 설정
- UniTask, R3, VContainer 및 NuGetForUnity
- DOTween 연동용 확장 코드(라이브러리는 미포함, 사용 시 별도 설치)
- `Assets/BKK/`: 이벤트, 오브젝트 풀, 업데이트 루프, UI 및 유틸리티 공용 코드

## 시작하기

1. 템플릿으로 생성한 프로젝트를 위 Unity 버전으로 열고 패키지 임포트를 완료합니다.
2. `Assets/Scenes/PassthroughSample.unity`를 열어 기본 구성을 확인합니다.
3. 기기 빌드는 `Assets/Settings/Build Profiles/Meta Quest.asset` 프로필을 사용합니다.
4. 새 프로젝트에 맞게 회사명·제품명·Android 패키지명과 Meta App ID(사용 시)를 변경하고, 빌드할 씬 목록을 정리합니다.

Git 기반 패키지 다운로드에는 Git과 인터넷 연결이 필요합니다. 포함된 외부 에셋·플러그인을 재배포할 때는 각 라이선스를 확인하세요. Meta VR Glasses 대상 사용은 별도 호환성 확인이 필요합니다.

참고: [Meta Building Blocks 공식 문서](https://developers.meta.com/vr/documentation/unity/unity-building-blocks-overview) (2026-11-10 확인)

## 저작권 및 라이선스

외부 SDK·라이브러리·에셋의 저작권은 각 권리자에게 있으며, 각 구성 요소의 라이선스가 적용됩니다.

- **R3 1.3.1**: Copyright (c) 2024 Cysharp, Inc. — MIT License. [라이선스 전문](Licenses/R3-LICENSE.txt)
- **Microsoft.Bcl.TimeProvider 8.0.0**, **System.ComponentModel.Annotations 5.0.0**, **System.Threading.Channels 8.0.0**: MIT License. 템플릿 내부 `Assets/Packages/`의 각 패키지에 포함된 `LICENSE.TXT`와 `THIRD-PARTY-NOTICES.TXT`를 유지하세요.
- **UniTask, VContainer, NuGetForUnity, Rider Editor, Visual Studio Editor**: UPM으로 설치되며 MIT License가 적용됩니다. 패키지 파일을 별도로 재배포할 때는 원본 저작권 고지와 라이선스를 함께 제공하세요.
- **Meta XR 및 Unity 패키지**: 각 패키지의 라이선스와 `NOTICE`·`Third Party Notices` 등 제3자 고지를 따르며, 복사한 코드·샘플의 원본 고지도 유지하세요.

`meta-xr-template.tgz`를 공유할 때는 이 README와 `Licenses/R3-LICENSE.txt`도 함께 제공하세요. 저작권 고지의 제공만으로 별도 재배포 권한이 부여되는 것은 아닙니다.
