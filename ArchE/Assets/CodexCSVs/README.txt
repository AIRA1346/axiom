ARCHÉ Codex CSV 입력 (ADDS)
============================

Tools > Codex > 3. Build All Codex Databases 실행 시
이 폴더 내의 모든 .csv 파일을 순회합니다.

필수 헤더(열 이름, 대소문자 무시 매칭):
  Id, Title, Lv1, Lv2, Lv3, Lv4, Lv5, Lv6, Lv7, Lv8, Lv9, Summary, Content

Id 규칙:
  KNO-[Lv1]-[Lv2]-[6자리 숫자]
  예: Lv1=BIO, Lv2=NAT 일 때 Id=KNO-BIO-NAT-000001
  Id의 두 번째·세 번째 구간은 반드시 Lv1·Lv2 열과 일치해야 합니다.

본문:
  Content는 Resources/CodexContents/[Id].txt 로 저장되며, 제목은 넣지 않습니다.
  Title(Lv10)과 Summary는 CodexIndex.bin 색인에만 기록됩니다.
