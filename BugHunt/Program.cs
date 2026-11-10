using System.Text;
using System.Text.Json;

namespace BugHunt;

// Hidden: 채점기가 돌릴 숨은 케이스(무작위 시드) 수. Release: Release 빌드에서도 판정해야 하는 랩
// [JA] Hidden: 採点器が回す隠しケース(ランダムなシード)の数。Release: Release ビルドでも判定しなければならないラボ
record LabEntry(string Title, string Symptom, Action Run, int Hidden = 0, bool Release = false);

static class Program
{
    // 제목과 증상은 Main 이 언어(--lang)를 정한 뒤, 처음 쓸 때 만든다
    // [JA] タイトルと症状は、Main が言語(--lang)を決めた後、最初に使うときに作ります
    static LabEntry[]? _labs;
    static LabEntry[] Labs => _labs ??=
    [
        // 1부: 예외와 스택 트레이스
        // [JA] パート 1: 例外とスタック トレース
        new(Text.L("친구 프로필", "フレンドのプロフィール"),      Text.L("세 번째 프로필에서 NullReferenceException", "3人目のプロフィールで NullReferenceException"),      Lab01.Run),
        new(Text.L("상점 장바구니", "ショップのカート"),    Text.L("세이브에서 불러온 장바구니 계산 중 죽는다", "セーブから読み込んだカートの計算中に落ちる"),       Lab02.Run),
        new(Text.L("죽은 적 정리", "倒した敵の片付け"),     Text.L("적이 쓰러지는 순간 컬렉션 예외가 난다", "敵が倒れた瞬間にコレクションの例外が出る"),          Lab03.Run),
        new(Text.L("서버 설정", "サーバー設定"),        Text.L("숫자 7을 int로 꺼내는데 InvalidCastException", "数値 7 を int で取り出すと InvalidCastException"),   Lab04.Run),
        new(Text.L("골드 지갑", "ゴールドの財布"),        Text.L("골드를 넣자마자 프로그램이 통째로 사라진다", "ゴールドを入れたとたんプログラムが丸ごと消える"),      Lab05.Run),
        new(Text.L("전리품 파일", "ドロップ品ファイル"),      Text.L("고장 난 파일 하나를 건너뛰게 했는데도 죽는다", "壊れたファイルを1つ飛ばすようにしたのに落ちる"),    Lab06.Run),
        new(Text.L("세이브 불러오기", "セーブの読み込み"),  Text.L("크래시 리포트가 엉뚱한 곳을 가리킨다", "クラッシュレポートが見当違いの場所を指す"),           Lab07.Run),
        new(Text.L("피해 라벨", "ダメージラベル"),        Text.L("피해 라벨을 만드는 순간 프로그램이 사라진다", "ダメージラベルを作った瞬間にプログラムが消える"),         Lab08.Run, Hidden: 3),
        // 2부: 흐름 추적
        // [JA] パート 2: 流れの追跡
        new(Text.L("스킬 단축키", "スキルのショートカット"),      Text.L("단축키 1, 2, 3이 전부 같은 스킬을 쓴다", "ショートカット 1, 2, 3 がすべて同じスキルを使う"),          Lab09.Run),
        new(Text.L("주간 랭킹 보상", "週間ランキング報酬"),   Text.L("1~3위가 보상을 두 번씩 받는다", "1~3位が報酬を2回ずつ受け取る"),                  Lab10.Run),
        new(Text.L("스태미나", "スタミナ"),         Text.L("달려도 스태미나가 줄지 않는다", "走ってもスタミナが減らない"),                  Lab11.Run),
        new(Text.L("설정 변경 감지", "設定変更の検出"),   Text.L("아무것도 안 바꿨는데 전부 바뀌었다고 한다", "何も変えていないのに全部変わったと言う"),       Lab12.Run),
        new(Text.L("경험치 바", "経験値バー"),        Text.L("바가 꽉 찼는데 레벨 업을 안 한다", "バーが満タンなのにレベルアップしない"),               Lab13.Run),
        new(Text.L("보스 기여도", "ボス貢献度"),      Text.L("제일 많이 때린 사람의 기여도가 음수다", "一番多く殴った人の貢献度がマイナスになる"),           Lab14.Run),
        new(Text.L("닉네임 필터", "ニックネームフィルター"),      Text.L("금칙어가 그대로 통과한다", "禁止ワードがそのまま通ってしまう"),                       Lab15.Run),
        new(Text.L("설정 파일", "設定ファイル"),        Text.L("독일 플레이어만 이동 속도가 10배가 된다", "ドイツのプレイヤーだけ移動速度が10倍になる"),           Lab16.Run),
        new(Text.L("이벤트 남은 시간", "イベント残り時間"), Text.L("남은 시간이 9시간 어긋난다", "残り時間が9時間ずれる"),                     Lab17.Run),
        new(Text.L("친구 목록", "フレンドリスト"),        Text.L("대소문자만 다른 이름이 친구 목록에 두 번 들어간다", "大文字小文字だけ違う名前がフレンドリストに2回入る"), Lab18.Run),
        new(Text.L("캐릭터 프리셋", "キャラクタープリセット"),    Text.L("복사본에 스킬을 넣었는데 원본도 바뀐다", "コピーにスキルを入れたのに元も変わる"),          Lab19.Run),
        new(Text.L("콤보", "コンボ"),           Text.L("몇 번을 맞춰도 콤보가 0이다", "何回合わせてもコンボが 0 のまま"),                         Lab20.Run, Hidden: 3),
        new(Text.L("길드원 번호", "ギルドメンバー番号"),    Text.L("새 길드원에게 보낸 환영 인사가 다른 사람에게 간다", "新メンバーに送った歓迎の挨拶が別の人に届く"),   Lab21.Run, Hidden: 3),
        new(Text.L("물약 단축키", "ポーションのショートカット"),    Text.L("물약이 다 떨어졌는데 계속 마실 수 있다", "ポーションが尽きたのに飲み続けられる"),              Lab22.Run, Hidden: 3),
        new(Text.L("아이콘 캐시", "アイコンキャッシュ"),    Text.L("아이콘 캐시를 켰는데 디스크 읽기가 그대로다", "アイコンキャッシュを有効にしたのにディスク読み込みが減らない"),         Lab23.Run, Hidden: 3),
        // 3부: 컨테이너와 컬렉션
        // [JA] パート 3: コンテナーとコレクション
        new(Text.L("퀘스트 정산", "クエスト精算"),      Text.L("퀘스트를 다 깨기 전에 정산하면 죽는다", "クエストを全部クリアする前に精算すると落ちる"),            Lab24.Run, Hidden: 3),
        new(Text.L("광역 공격", "範囲攻撃"),        Text.L("광역 공격을 맞아도 파티 HP가 그대로다", "範囲攻撃を受けてもパーティーの HP がそのまま"),            Lab25.Run, Hidden: 3),
        new(Text.L("길드 창고", "ギルド倉庫"),        Text.L("길드 태그를 바꾸자 창고가 사라졌다", "ギルドタグを変えたら倉庫が消えた"),               Lab26.Run, Hidden: 3),
        new(Text.L("처치 기록", "討伐記録"),        Text.L("처치 기록 수가 매번 모자라고 가끔 예외가 난다", "討伐記録の数が毎回足りず、ときどき例外が出る"),     Lab27.Run, Hidden: 2),
        new(Text.L("시즌 리더보드", "シーズン リーダーボード"),    Text.L("점수가 같으면 먼저 달성한 사람이 밀려난다", "スコアが同じだと先に達成した人が後ろに押し出される"),         Lab28.Run, Hidden: 3),
        new(Text.L("드롭 테이블", "ドロップテーブル"),      Text.L("고장 난 파일을 건너뛰게 했는데 다른 곳에서 죽는다", "壊れたファイルを飛ばすようにしたのに、別の場所で落ちる"), Lab29.Run, Hidden: 3),
        new(Text.L("레이드 보상", "レイド報酬"),      Text.L("레이드에 없던 사람이 보상을 받는다", "レイドにいなかった人が報酬を受け取る"),               Lab30.Run, Hidden: 3),
        // 4부: C# 언어 함정
        // [JA] パート 4: C# 言語の落とし穴
        new(Text.L("즐겨찾기 스킬", "お気に入りスキル"),    Text.L("재접속하면 즐겨찾기 별표가 전부 사라진다", "再接続するとお気に入りの星がすべて消える"),            Lab31.Run, Hidden: 3),
        new(Text.L("콤보 보너스", "コンボボーナス"),      Text.L("콤보가 없는 공격은 피해가 0으로 나온다", "コンボのない攻撃はダメージが 0 になる"),              Lab32.Run, Hidden: 3),
        new(Text.L("드롭 등급", "ドロップ等級"),        Text.L("모르는 등급의 아이템이 일반 등급처럼 쏟아진다", "知らない等級のアイテムがノーマル等級のように大量に出る"),       Lab33.Run, Hidden: 3),
        new(Text.L("턴 순서", "ターン順"),          Text.L("모든 턴을 첫 번째 유닛만 가져간다", "すべてのターンを最初のユニットだけが持っていく"),                   Lab34.Run, Hidden: 3),
        new(Text.L("자동 저장 잠금", "オートセーブのロック"),   Text.L("자동 저장이 도는 동안 업적 집계가 멈춘다", "オートセーブが動いている間、実績集計が止まる"),            Lab35.Run, Hidden: 3),
        new(Text.L("패치 다운로드", "パッチ ダウンロード"),    Text.L("받은 조각 번호가 뒤죽박죽이고 범위 밖까지 섞인다", "受け取った断片番号がばらばらで、範囲外の番号まで混ざる"),    Lab36.Run, Hidden: 3),
        new(Text.L("캐시 상점", "キャッシュショップ"),        Text.L("합계가 조금씩 어긋나고 세금 반올림이 내려간다", "合計が少しずつずれ、税の四捨五入が切り捨てになる"),       Lab37.Run, Hidden: 3),
        // 5부: 인터페이스와 상속
        // [JA] パート 5: インターフェイスと継承
        new(Text.L("스킬 피해", "スキルダメージ"),        Text.L("불 스킬을 써도 화상 보너스가 안 붙는다", "炎スキルを使っても火傷ボーナスが付かない"),             Lab38.Run, Hidden: 3),
        new(Text.L("드롭 개수", "ドロップ数"),        Text.L("보스를 잡아도 아이템이 하나만 떨어진다", "ボスを倒してもアイテムが一つしか落ちない"),             Lab39.Run, Hidden: 3),
        new(Text.L("퀘스트 보상", "クエスト報酬"),      Text.L("이벤트 배율이 정산에서는 빠진다", "イベント倍率が精算では抜け落ちる"),                    Lab40.Run, Hidden: 3),
        new(Text.L("버프 지속 시간", "バフ持続時間"),   Text.L("턴을 넘겨도 버프가 사라지지 않는다", "ターンを進めてもバフが消えない"),                 Lab41.Run, Hidden: 3),
        new(Text.L("유닛 소환", "ユニット召喚"),        Text.L("기사가 갑옷 보정 없는 체력으로 소환된다", "騎士が鎧補正なしのHPで召喚される"),            Lab42.Run, Hidden: 3),
        new(Text.L("즐겨찾기", "お気に入り"),         Text.L("가방에는 있다는 아이템이 즐겨찾기에서는 없다", "バッグにはあるはずのアイテムがお気に入りではないことになる"),       Lab43.Run, Hidden: 3),
        new(Text.L("처치 보상", "討伐報酬"),        Text.L("보스를 잡았는데 일반 적만큼만 준다", "ボスを倒したのに通常の敵と同じ額しかもらえない"),                 Lab44.Run, Hidden: 3),
        new(Text.L("시작 가방", "初期バッグ"),        Text.L("물약을 넣는 순간 ArrayTypeMismatchException", "ポーションを入れた瞬間に ArrayTypeMismatchException"),        Lab45.Run, Hidden: 3),
        // 6부: 파일 입출력
        // [JA] パート 6: ファイル入出力
        new(Text.L("세이브 폴더", "セーブフォルダー"),      Text.L("폴더를 한 번 열고 나면 세이브가 없다고 한다", "フォルダーを一度開くとセーブがないと言われる"),         Lab46.Run, Hidden: 3),
        new(Text.L("전투 기록 파일", "戦闘ログファイル"),   Text.L("저장한 전투 기록의 마지막 몇 줄이 빠져 있다", "保存した戦闘ログの最後の数行が欠けている"),         Lab47.Run, Hidden: 3),
        new(Text.L("채팅 신고", "チャット通報"),        Text.L("신고하면 채팅 로그를 '사용 중'이라 못 읽는다", "通報するとチャットログが「使用中」で読めない"),        Lab48.Run, Hidden: 3),
        new(Text.L("프로필 가져오기", "プロフィール取り込み"),  Text.L("가져온 프로필의 세이브만 백업에서 빠진다", "取り込んだプロフィールのセーブだけバックアップから漏れる"),            Lab49.Run, Hidden: 3),
        new(Text.L("NPC 대사 모드", "NPC セリフ MOD"),    Text.L("모드에 따라 첫 대사가 없거나 한글이 깨진다", "MOD によって最初のセリフがなかったり、セリフが文字化けしたりする"),          Lab50.Run, Hidden: 3),
        new(Text.L("차단 목록", "ブロックリスト"),        Text.L("메모장으로 고친 뒤 아무도 차단되지 않는다", "メモ帳で編集してから誰もブロックされない"),            Lab51.Run, Hidden: 3),
        new(Text.L("안전한 저장", "安全なセーブ"),      Text.L("저장이 실패하자 지난번 세이브까지 망가졌다", "保存に失敗したら前回のセーブまで壊れた"),          Lab52.Run, Hidden: 3),
        new(Text.L("클라우드 동기화", "クラウド同期"),  Text.L("서버에는 늘 한 번 전의 설정이 올라간다", "サーバーにはいつも一つ前の設定がアップロードされる"),              Lab53.Run, Hidden: 3),
        // 7부: 직렬화 (JSON/XML)
        // [JA] パート 7: シリアル化 (JSON/XML)
        new(Text.L("길드 공지", "ギルドのお知らせ"),        Text.L("예외도 없는데 공지가 전부 빈 칸이다", "例外も出ないのにお知らせがすべて空欄になる"),                 Lab54.Run, Hidden: 3),
        new(Text.L("캐릭터 세이브", "キャラクターのセーブ"),    Text.L("불러오면 골드는 0, 레벨은 1이 된다", "ロードするとゴールドは 0、レベルは 1 になる"),                  Lab55.Run, Hidden: 3),
        new(Text.L("우편 보상", "メール報酬"),        Text.L("첨부한 보상이 전부 '알 수 없는 보상'이 된다", "添付した報酬がすべて「不明な報酬」になる"),         Lab56.Run, Hidden: 3),
        new(Text.L("파티 스냅샷", "パーティースナップショット"),      Text.L("복원 뒤 리더가 맞아도 파티 창 체력은 그대로다", "復元後にリーダーがダメージを受けてもパーティー画面のHPが変わらない"),       Lab57.Run, Hidden: 3),
        new(Text.L("보상 패킷", "報酬パケット"),        Text.L("이상한 패킷을 거절하지 않고 x0 보상이 들어온다", "おかしなパケットを拒否せず x0 の報酬が入ってくる"),      Lab58.Run, Hidden: 3),
        new(Text.L("옵션 XML", "オプション XML"),         Text.L("그래픽 옵션을 추가하자 저장하는 순간 죽는다", "グラフィックオプションを追加したら保存した瞬間に落ちる"),         Lab59.Run, Hidden: 3),
        new(Text.L("퀘스트 XML", "クエスト XML"),       Text.L("XML에 스키마를 붙인 뒤로 보상이 전부 0G다", "XML にスキーマを付けてから報酬がすべて 0G になる"),            Lab60.Run, Hidden: 3),
        new(Text.L("길드 초대장", "ギルド招待状"),      Text.L("어떤 길드의 초대장은 보내도 도착하지 않는다", "あるギルドの招待状は送っても届かない"),         Lab61.Run, Hidden: 3),
        // 8부: 비동기와 동시성
        // [JA] パート 8: 非同期と並行処理
        new(Text.L("자동 저장", "オートセーブ"),        Text.L("try/catch로 감쌌는데 저장 실패로 죽는다", "try/catch で囲んだのに、セーブ失敗で落ちる"),         Lab62.Run),
        new(Text.L("로딩 화면", "ロード画面"),        Text.L("로딩 바가 100%에서 멈춘다", "ロードバーが 100% で止まる"),                      Lab63.Run),
        new(Text.L("업적 전송", "実績送信"),        Text.L("업적을 세 개 보냈는데 서버는 0개 받았다", "実績を三つ送ったのに、サーバーは 0 個しか受け取っていない"),         Lab64.Run),
        new(Text.L("패치 다운로드", "パッチ ダウンロード"),    Text.L("두 개가 실패했는데 로그에는 하나만 남는다", "二つ失敗したのに、ログには一つしか残らない"),       Lab65.Run),
        new(Text.L("파티 피해 집계", "パーティー ダメージ集計"),   Text.L("총 피해량이 매번 다르다", "総ダメージ量が毎回違う"),                        Lab66.Run),
        new(Text.L("HUD 갱신", "HUD 更新"),         Text.L("로딩 후 HUD가 엉뚱한 스레드에서 갱신된다", "ロード後、HUD が見当違いのスレッドで更新される"),        Lab67.Run),
        new(Text.L("매칭 취소", "マッチング キャンセル"),        Text.L("취소를 눌렀는데 매칭이 계속된다", "キャンセルを押したのに、マッチングが続く"),                Lab68.Run),
        new(Text.L("개인 창고", "個人倉庫"),      Text.L("창고에 맡긴 아이템이 다시 접속하면 사라진다", "倉庫に預けたアイテムが、再接続すると消えている"),     Lab69.Run, Hidden: 3),
        new(Text.L("던전 입장", "ダンジョン入場"),      Text.L("취소를 누르지 않았는데 '취소됨'이 뜬다", "キャンセルを押していないのに「キャンセル済み」と表示される"),          Lab70.Run, Hidden: 3),
        new(Text.L("퀘스트 동기화", "クエスト同期"),  Text.L("오류 보고서가 늘 재시도 함수만 가리킨다", "エラーレポートがいつも再試行関数だけを指す"),          Lab71.Run, Hidden: 3),
        // 9부: 리소스와 메모리
        // [JA] パート 9: リソースとメモリ
        new(Text.L("리플레이 저장", "リプレイ保存"),    Text.L("저장한 리플레이 파일이 비어 있다", "保存したリプレイ ファイルが空になっている"),               Lab72.Run),
        new(Text.L("전투 기록 창", "戦闘ログ ウィンドウ"),     Text.L("닫은 창들이 메모리에서 사라지지 않는다", "閉じたウィンドウがメモリから消えない"),          Lab73.Run),
        new(Text.L("텍스처 캐시", "テクスチャ キャッシュ"),      Text.L("레벨을 나갔는데 텍스처 메모리가 그대로다", "レベルを出たのに、テクスチャ メモリがそのまま"),        Lab74.Run),
        // 10부: unsafe와 네이티브 메모리
        // [JA] パート 10: unsafe とネイティブ メモリ
        new(Text.L("미니맵 경로", "ミニマップの経路"),      Text.L("경로를 구했는데 미니맵에는 엉뚱한 타일이 찍힌다", "経路を求めたのに、ミニマップには見当違いのタイルが描かれる"),  Lab75.Run, Hidden: 3, Release: true),
        new(Text.L("효과음 믹서", "効果音ミキサー"),      Text.L("레벨을 넘어가면 효과음 볼륨이 엉망이 된다", "レベルが切り替わると効果音の音量がめちゃくちゃになる"),       Lab76.Run, Hidden: 3, Release: true),
        new(Text.L("패킷 수신", "パケット受信"),        Text.L("손상된 패킷을 버렸는데 메모리가 안 줄어든다", "破損したパケットを捨てたのにメモリが減らない"),      Lab77.Run, Hidden: 3),
        new(Text.L("박자 콜백", "ビート コールバック"),        Text.L("곡을 고르면 게임이 통째로 사라진다", "曲を選ぶとゲームが丸ごと消える"),              Lab78.Run, Hidden: 2, Release: true),
        new(Text.L("채팅 기록", "チャット履歴"),        Text.L("채팅 기록이 전부 마지막 메시지로 바뀐다", "チャット履歴がすべて最後のメッセージに変わる"),          Lab79.Run, Hidden: 3),
        new(Text.L("패킷 헤더", "パケット ヘッダー"),        Text.L("서버가 보낸 채팅이 하나도 안 보인다", "サーバーが送ったチャットが一つも表示されない"),              Lab80.Run, Hidden: 3),
        new(Text.L("패킷 버퍼", "パケットバッファ"),        Text.L("큰 패킷이 오면 프로그램이 통째로 사라진다", "大きなパケットが来るとプログラムが丸ごと消える"),            Lab81.Run, Hidden: 3),
        new(Text.L("콤보 버퍼", "コンボバッファ"),        Text.L("콤보가 넘치면 옆 칸 값이 저절로 바뀐다", "コンボがあふれると隣のマスの値が勝手に変わる"),              Lab82.Run, Hidden: 3),
        // 11부: 멈춤 - 무한루프와 데드락
        // [JA] パート 11: 停止 - 無限ループとデッドロック
        new(Text.L("보스 등장 연출", "ボス登場演出"),   Text.L("보스가 나오면 카메라 줌이 끝나지 않는다", "ボスが出るとカメラのズームが終わらない"),          Lab83.Run, Hidden: 3),
        new(Text.L("인벤토리 저장", "インベントリの保存"),    Text.L("확장 가방을 산 플레이어만 저장하다 멈춘다", "拡張バッグを買ったプレイヤーだけ保存中に止まる"),        Lab84.Run, Hidden: 3),
        new(Text.L("길드 동맹", "ギルド同盟"),        Text.L("동맹 가입 뒤 맹주 찾기가 끝나지 않는다", "同盟加入後、盟主探しが終わらない"),           Lab85.Run, Hidden: 3),
        new(Text.L("동시 거래", "同時取引"),        Text.L("두 사람이 서로 골드를 보내면 둘 다 멈춘다", "二人がお互いにゴールドを送ると二人とも止まる"),         Lab86.Run, Hidden: 2),
        new(Text.L("패치 슬롯", "パッチ スロット"),        Text.L("몇 개 실패하더니 다운로드가 통째로 멈춘다", "いくつか失敗したと思ったら、ダウンロードが丸ごと止まる"),         Lab87.Run, Hidden: 3),
        new(Text.L("로딩 대기", "ロード待ち"),        Text.L("Release 출시판에서만 로딩 화면이 끝나지 않는다", "Release の製品版でだけロード画面が終わらない"),   Lab88.Run, Hidden: 1, Release: true),
        // 12부: 성능 - 틀리지는 않는데 느리다
        // [JA] パート 12: 性能 - 間違ってはいないのに遅い
        new(Text.L("위협도 표시", "脅威度表示"),      Text.L("전투가 길어지면 프레임이 툭툭 끊긴다", "戦闘が長引くとフレームがカクカク途切れる"),               Lab89.Run, Hidden: 2),
        new(Text.L("채팅 내보내기", "チャットの書き出し"),    Text.L("기록이 길면 내보내기가 한참 걸린다", "履歴が長いと書き出しにかなり時間がかかる"),                 Lab90.Run, Hidden: 2),
        new(Text.L("전투 통계", "戦闘統計"),        Text.L("통계를 켜면 GC 횟수가 확 늘어난다", "統計をオンにすると GC の回数がぐっと増える"),                  Lab91.Run, Hidden: 2),
        new(Text.L("전리품 도감", "戦利品図鑑"),      Text.L("큰 레이드가 끝나면 몇 초씩 멈춘다", "大きなレイドが終わると数秒ずつ止まる"),                  Lab92.Run, Hidden: 2),
        new(Text.L("이름 검사", "名前チェック"),        Text.L("채팅이 몰리면 서버 CPU가 치솟는다", "チャットが集中するとサーバーの CPU が跳ね上がる"),                  Lab93.Run, Hidden: 2),
        new(Text.L("디버그 로그", "デバッグ ログ"),      Text.L("로그를 꺼 뒀는데도 이동 처리 때 GC가 돈다", "ログをオフにしてあるのに、移動処理のときに GC が走る"),          Lab94.Run, Hidden: 2),
        new(Text.L("주사위 명령", "ダイス コマンド"),      Text.L("이상한 명령을 도배하면 서버가 느려진다", "変なコマンドを連投されるとサーバーが遅くなる"),             Lab95.Run, Hidden: 2),
    ];

    // 챌린지: 힌트 없음. 기본 랩 번호와 따로 센다. C-n = 깊은 추적, B-n = 블라인드
    // [JA] チャレンジ: ヒントなし。基本ラボの番号とは別に数えます。C-n = 深い追跡、B-n = ブラインド
    record ChallengeEntry(string Code, string Symptom, Action Run, int Hidden, bool Release = false)
    {
        // --list 와 채점 보고서의 id. 정수를 유지하려고 C-n은 100+n, B-n은 200+n
        // [JA] --list と採点レポートの id。整数のままにするため C-n は 100+n、B-n は 200+n
        public int Id => (Code[0] == 'C' ? 100 : 200) + int.Parse(Code[2..]);
        public string Title => Text.L($"챌린지 {Code}", $"チャレンジ {Code}");
    }

    static ChallengeEntry[]? _challenges;
    static ChallengeEntry[] Challenges => _challenges ??=
    [
        new("C-1", Text.L("레이드 보상이 가방에 여러 번 들어온다", "レイドの報酬がバッグに何度も入ってくる"),                 ChallengeC1.Run, Hidden: 4),
        new("C-2", Text.L("로그인하자마자 잡은 몬스터가 엉뚱한 업적에 올라간다", "ログイン直後に倒したモンスターが見当違いの実績に加算される"),   ChallengeC2.Run, Hidden: 4),
        new("C-3", Text.L("친구 랭킹을 본 뒤에 주간 보상 우편이 엉뚱하게 나간다", "フレンド ランキングを見たあとに、週間報酬の郵便がおかしな相手に送られる"),  ChallengeC3.Run, Hidden: 4),
        new("B-1", Text.L("지역을 불러오다가 멈춘다", "地域を読み込む途中で止まる"),                              ChallengeB1.Run, Hidden: 2),
        new("B-2", Text.L("터키 서버에서만 일부 채팅 명령이 안 먹는다", "トルコ サーバーでだけ一部のチャット コマンドが効かない"),            ChallengeB2.Run, Hidden: 3),
        new("B-3", Text.L("맵이 커질수록 길찾기가 급격히 느려진다", "マップが大きくなるほど経路探索が急激に遅くなる"),                ChallengeB3.Run, Hidden: 2),
        new("B-4", Text.L("전투가 끝나고 부대를 채우면 죽는다", "戦闘が終わって部隊を補充すると落ちる"),                    ChallengeB4.Run, Hidden: 3),
        new("B-5", Text.L("자동 저장이 처음 몇 번만 되고 멈춘다", "オートセーブが最初の数回だけ行われて止まる"),                  ChallengeB5.Run, Hidden: 2),
        new("B-6", Text.L("하드 모드인데 시작 골드가 이상하다", "ハード モードなのに開始ゴールドがおかしい"),                    ChallengeB6.Run, Hidden: 2),
        new("B-7", Text.L("보상 우편이 '발송 완료' 뒤에 도착한다", "補償の郵便が「発送完了」のあとに届く"),                 ChallengeB7.Run, Hidden: 2),
        new("B-8", Text.L("출시판에서만 텍스처 미리보기가 가끔 깨진다", "製品版でだけテクスチャのプレビューがときどき壊れる"),            ChallengeB8.Run, Hidden: 3, Release: true),
    ];

    const int ChallengePart = 0;   // --list 의 part 값. 번호 붙은 부가 아니라는 뜻. 채점기는 "챌린지"로 표시한다 / --list の part の値。番号の付いたパートではないという意味。採点器は「チャレンジ」と表示します

    const int Part2Start = 9, Part3Start = 24, Part4Start = 31, Part5Start = 38, Part6Start = 46, Part7Start = 54, Part8Start = 62, Part9Start = 72, Part10Start = 75, Part11Start = 83, Part12Start = 89;

    static string BuildName =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    static void PrintMenu()
    {
        Console.WriteLine(Text.L($"=== BugHunt: C# 버그 추적 실습 [{BuildName}] ===", $"=== BugHunt: C# バグ追跡演習 [{BuildName}] ==="));
        for (int i = 0; i < Labs.Length; i++)
        {
            int n = i + 1;
            if (n == 1) Console.WriteLine(Text.L("\n[1부] 예외와 스택 트레이스 - VS에서 F5로 실행하세요", "\n[第1部] 例外とスタックトレース - VS で F5 で実行してください"));
            else if (n == Part2Start) Console.WriteLine(Text.L("\n[2부] 흐름 추적 - 크래시 없음. 중단점을 걸고 F10/F11로 따라가세요", "\n[第2部] 流れの追跡 - クラッシュなし。ブレークポイントを置いて F10/F11 で追ってください"));
            else if (n == Part3Start) Console.WriteLine(Text.L("\n[3부] 컨테이너와 컬렉션 - 예외 도우미, 조사식, 개체 ID", "\n[第3部] コンテナーとコレクション - 例外ヘルパー、ウォッチ、オブジェクト ID"));
            else if (n == Part4Start) Console.WriteLine(Text.L("\n[4부] C# 언어 함정 - 식을 쪼개 조사식에 하나씩 넣어 보세요", "\n[第4部] C# 言語の罠 - 式を分けて一つずつウォッチに入れてみてください"));
            else if (n == Part5Start) Console.WriteLine(Text.L("\n[5부] 인터페이스와 상속 - 조사식의 실제 형식, 가상 호출 따라가기", "\n[第5部] インターフェイスと継承 - ウォッチで実際の型を見る、仮想呼び出しを追う"));
            else if (n == Part6Start) Console.WriteLine(Text.L("\n[6부] 파일 입출력 - 경로, 공유 모드, 인코딩, 반쯤 쓴 파일", "\n[第6部] ファイル入出力 - パス、共有モード、エンコーディング、書きかけのファイル"));
            else if (n == Part7Start) Console.WriteLine(Text.L("\n[7부] 직렬화 (JSON/XML) - 저장한 것과 불러온 것을 나란히 비교하세요", "\n[第7部] シリアル化 (JSON/XML) - 保存したものと読み込んだものを並べて比べてください"));
            else if (n == Part8Start) Console.WriteLine(Text.L("\n[8부] 비동기와 동시성 - 병렬 스택/작업 창", "\n[第8部] 非同期と並行処理 - 並列スタック / タスク ウィンドウ"));
            else if (n == Part9Start) Console.WriteLine(Text.L("\n[9부] 리소스와 메모리 (선택) - 진단 도구, 메모리 스냅샷", "\n[第9部] リソースとメモリ (任意) - 診断ツール、メモリ スナップショット"));
            else if (n == Part10Start) Console.WriteLine(Text.L("\n[10부] unsafe와 네이티브 메모리 (선택) - 메모리 창, 조사식의 포인터 식", "\n[第10部] unsafe とネイティブ メモリ (任意) - メモリ ウィンドウ、ウォッチでのポインター式"));
            else if (n == Part11Start) Console.WriteLine(Text.L("\n[11부] 멈춤: 무한루프와 데드락 - 모두 중단, 병렬 스택, 스레드 창, dotnet-stack", "\n[第11部] 停止: 無限ループとデッドロック - すべて中断、並列スタック、スレッド ウィンドウ、dotnet-stack"));
            else if (n == Part12Start) Console.WriteLine(Text.L("\n[12부] 성능 - 틀리지는 않는데 느리다. 성능 프로파일러, dotnet-counters, dotnet-trace", "\n[第12部] 性能 - 間違ってはいないが遅い。パフォーマンス プロファイラー、dotnet-counters、dotnet-trace"));
            Console.WriteLine($"  {n,2}. {Labs[i].Title}  - {Labs[i].Symptom}");
        }
        Console.WriteLine(Text.L("\n[챌린지] 힌트 없음. 1~12부를 먼저 끝내세요. C: 여러 파일을 거치는 깊은 추적, B: 범주를 알려 주지 않는 블라인드", "\n[チャレンジ] ヒントなし。先に第1~12部を終えてください。C: 複数のファイルをまたぐ深い追跡、B: 分類を明かさないブラインド"));
        foreach (ChallengeEntry c in Challenges)
            Console.WriteLine($"  {c.Code}. {c.Symptom}");
        Console.WriteLine(Text.L("  (명령줄 인자로 번호를 넘겨도 된다: BugHunt.exe 3, BugHunt.exe C-1)", "  (コマンドライン引数で番号を渡してもかまいません: BugHunt.exe 3, BugHunt.exe C-1)"));
        Console.WriteLine(Text.L("  (채점: 저장소 폴더에서 grade.bat  또는  grade.bat 24-30,  grade.bat C)", "  (採点: リポジトリのフォルダーで grade.bat  または  grade.bat 24-30,  grade.bat C)"));
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 출력 언어: --lang ja|ko > BUGHUNT_LANG > 화면 언어(CurrentUICulture) > 한국어
        // [JA] 出力言語: --lang ja|ko > BUGHUNT_LANG > 画面の言語(CurrentUICulture) > 韓国語
        string[]? rest = Text.TakeLangArg(args);
        if (rest == null)
        {
            Console.WriteLine("--lang 뒤에는 ko 또는 ja / --lang の後には ko か ja を指定してください"); // ko+ja (언어를 아직 못 골랐다 / まだ言語が決まっていない)
            return 2;
        }
        args = rest;
        if (args.Length > 0 && args[0] == "--check-lang")
            return CheckLang();

        if (args.Length > 0 && args[0] == "--list")
            return ListLabs();
        if (args.Length > 0 && args[0] == "grade")
        {
            Console.WriteLine(Text.L("채점기는 별도 프로젝트로 옮겼습니다: grade.bat 또는 dotnet run --project BugHunt.Grader (README의 \"채점기\" 참고)", "採点ツールは別プロジェクトに移しました: grade.bat または dotnet run --project BugHunt.Grader (README の「採点ツール」を参照)"));
            return 2;
        }

        // 숨은 케이스 재현: BugHunt.exe 84 --seed 1234
        // [JA] 隠しケースの再現: BugHunt.exe 84 --seed 1234
        int seedAt = Array.IndexOf(args, "--seed");
        if (seedAt >= 0 && seedAt + 1 < args.Length && int.TryParse(args[seedAt + 1], out int seed))
        {
            Seed = seed;
            args = [.. args[..seedAt], .. args[(seedAt + 2)..]];
        }

        string? input;
        if (args.Length > 0)
        {
            input = args[0];
        }
        else
        {
            PrintMenu();
            Console.WriteLine();
            Console.Write(Text.L("실행할 랩 번호: ", "実行するラボ番号: "));
            input = Console.ReadLine();
        }

        input = input?.Trim().ToUpperInvariant();
        ChallengeEntry? challenge = Challenges.FirstOrDefault(c => c.Code == input || c.Id.ToString() == input);
        string heading;
        Action run;
        if (challenge != null)
        {
            heading = challenge.Title;
            run = challenge.Run;
        }
        else if (int.TryParse(input, out int choice) && choice >= 1 && choice <= Labs.Length)
        {
            heading = $"Lab {choice}: {Labs[choice - 1].Title}";
            run = Labs[choice - 1].Run;
        }
        else
        {
            Console.WriteLine(Text.L($"1 ~ {Labs.Length} 사이의 번호나 챌린지 코드(C-1, B-1 ...)를 입력하세요.", $"1 ~ {Labs.Length} の番号かチャレンジ コード(C-1, B-1 ...)を入力してください。"));
            return 2;
        }

        string hiddenCase = Seed != 0 ? " " + Text.L($"(숨은 케이스 seed {Seed})", $"(隠しケース seed {Seed})") : "";
        Console.WriteLine($"\n--- {heading} [{BuildName}]{hiddenCase} ---");
        run();
        Console.WriteLine(Text.L("--- 종료 ---", "--- 終了 ---"));
        return FailCount == 0 && PassCount > 0 ? 0 : 1;
    }

    // 채점기(BugHunt.Grader)가 랩 목록을 읽어 가는 출력
    // [JA] 採点器(BugHunt.Grader)がラボ一覧を読み取るための出力
    static int ListLabs()
    {
        var labs = Labs.Select((lab, i) => new
        {
            Id = i + 1,
            lab.Title,
            Part = PartOf(i + 1),
            lab.Hidden,
            lab.Release,
        }).Concat(Challenges.Select(c => new { c.Id, c.Title, Part = ChallengePart, c.Hidden, c.Release }));
        Console.WriteLine(JsonSerializer.Serialize(labs, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return 0;
    }

    // 번역 검사: 소스에서 Text.L(...) 밖이나 일본어 쪽(둘째 인자)에 남은 한글 문자열 리터럴을 찾는다. (주석과 "// ko+ja" 표시가 붙은 줄은 제외)
    // 일본어 짝([JA] 줄)이 없는 한글 주석도 찾는다.
    // 랩을 실행할 때 일본어 출력에 한글이 섞이는지는 채점기 --lang ja 가 랩마다 본다.
    // [JA] 翻訳チェック: ソースで Text.L(...) の外か日本語側(2 番目の引数)に残ったハングルの文字列リテラルを探します。(コメントと "// ko+ja" の付いた行は除外)
    // [JA] 日本語のペア([JA] の行)がないハングルのコメントも探します。
    // [JA] ラボ実行時に日本語の出力にハングルが混じるかどうかは、採点器の --lang ja がラボごとに見ます。
    static int CheckLang()
    {
        string? root = null;
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir != null && root == null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "BugHunt.slnx")))
                root = dir.FullName;
        if (root == null)
        {
            Console.WriteLine(Text.L("BugHunt.slnx가 있는 폴더를 찾지 못했습니다. 저장소 안에서 실행하세요.", "BugHunt.slnx があるフォルダーが見つかりませんでした。リポジトリの中で実行してください。"));
            return 2;
        }
        List<string> unpaired = Text.FindUnpairedHangul(root);
        foreach (string u in unpaired)
            Console.WriteLine(u);
        int comments = unpaired.Count(u => u.Contains(": " + Text.CommentTag, StringComparison.Ordinal));
        int literals = unpaired.Count - comments;
        Console.WriteLine(Text.L($"check-lang: Text.L 밖이나 일본어 쪽에 남은 한글 문자열 {literals}개, 일본어 짝이 없는 한글 주석 {comments}개", $"check-lang: Text.L の外か日本語側に残ったハングル文字列 {literals} 個、日本語のペアがないハングルのコメント {comments} 個"));
        return unpaired.Count == 0 ? 0 : 1;
    }

    static int PartOf(int n) =>
        n >= Part12Start ? 12 : n >= Part11Start ? 11 : n >= Part10Start ? 10 : n >= Part9Start ? 9 : n >= Part8Start ? 8 : n >= Part7Start ? 7 : n >= Part6Start ? 6 : n >= Part5Start ? 5 : n >= Part4Start ? 4 : n >= Part3Start ? 3 : n >= Part2Start ? 2 : 1;
}
