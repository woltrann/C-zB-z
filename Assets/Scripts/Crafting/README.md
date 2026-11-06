# Crafting kurulumu

Sistem mevcut `PlayerInteraction` E etkileşimini, `Inventory` verisini ve `WorldItem` F toplamasını kullanır. Tarifler `CraftStation` Inspector listesindedir; yeni tarif için C# değiştirmek gerekmez.

## İlk kurulum

GameScene'de aşağıdaki nesneler hazırlandı. Sahne kullanıcı isteği gereği otomatik kaydedilmedi; düzenlemeleri kalıcı tutmak için sahneyi kullanıcı kaydetmelidir.

```text
CuttingStation                 CraftStation, Outline; layer 6
├── Model                      Cube, Collider; layer 6
└── OutputPoint                Transform
Canvas
└── CraftingUI                 CraftingUI; mevcut Canvas altında tek örnek
    └── CraftingPanel           Kapalı başlar
        └── Window             Tarif listesi, malzemeler, ilerleme ve butonlar
```

`CraftingUI` görünümü `Assets/Prefab/UI/CraftingUI.prefab` olarak kaydedildi. Inspector referansları prefabda bağlıdır; font ve panel/buton sprite'ları mevcut maanetorn UI paketindendir. Yeni tezgâh başına Canvas veya UI kopyalanmaz.

İki tarifi ve model/çıkış referansları bağlı tezgâh ayrıca `Assets/Prefab/Crafting/CuttingStation.prefab` olarak kaydedildi. Sahne kaydedilmeden kapatılsa bile bu prefab ve ortak UI prefabı yeniden sahneye eklenebilir.

CuttingStation alanları:

| Alan | Değer |
| --- | --- |
| Station Name | Cutting Station |
| Interaction Distance | 2.5 |
| Output Point | CuttingStation/OutputPoint |
| Output Spacing | 1 |
| Outline | Kök nesnenin Outline bileşeni |
| Completion Effect | İsteğe bağlı; boş bırakıldı |

| Recipe Name | Output World Prefab | Output Amount | Craft Duration | Required Materials |
| --- | --- | --- | --- | --- |
| Lumber | Assets/Prefab/DropPrefab/Lumber.prefab | 2 | 2 saniye | Assets/Data/Resource/treeTrunkData.asset × 1 |
| Pole | Assets/Prefab/DropPrefab/Pole.prefab | 3 | 1.5 saniye | Assets/Data/Crafting/LumberData.asset × 1 |

Lumber ve Pole için mevcut `Lumber.fbx` içindeki gerçek, ayrı meshler kullanıldı. Yeni ItemData varlıkları `Assets/Data/Crafting/` altındadır. Ürün prefabları WorldItem, Rigidbody, BoxCollider, ItemFall ve Outline taşır; toplama layer'ı 7, başlangıç miktarı 1'dir. İkonları bu modellerden render edildi.

## Kullanım

1. Tezgâha yaklaş, E ile ortak paneli aç.
2. Tarif seç, Tarifi Seç ile hazırlama aşamasına geç.
3. Malzeme Ekle ile envanterden tezgâha aktar; Geri Al ile iade et.
4. Tüm malzemeler tamamlanınca görünen Üret butonuyla üretimi başlat.
5. Çıkan parçaları mevcut F etkileşimiyle tek tek topla.

ESC veya kapat butonu paneli kapatır; hazırlama ve rezervler korunur. Üretim panel kapalıyken devam eder. Malzeme yüklenmişken tarif değiştirme engellenir; önce malzemeler Geri Al ile iade edilmelidir. İptal bütün rezervleri atomik olarak iade eder; envanter kapasitesi yetersizse işlem engellenir ve rezervler korunur. Ürünlerin tamamı güvenli biçimde yerleştirilemezse kısmi çıktılar temizlenir, rezervler tüketilmez.

## Yeni tezgâh

Aynı kök/model/OutputPoint yapısına CraftStation ve isteğe bağlı Outline ekle. Collider nesnesinin layer'ı oyuncunun Interaction Layer maskesine dahil olmalı (mevcut sahnede 6). OutputPoint'i zemine yakın ve modelin dışında tut; yerleştirme aşağıya zemin arar. Interaction Distance, Output Spacing ve Recipes listesini Inspector'dan ayarla. Her ürün prefabında etkin WorldItem, ItemData ve Collider gerekir. Prefab layer'ı oyuncunun WorldItem maskesinde bulunmalıdır. Aynı malzemeyi bir tarifte tekrar etme; toplam miktarı tek satırda gir.

## Kalıcılık sınırı

Projede hazır kayıt/yükleme altyapısı bulunmadı. Rezervler yalnızca mevcut çalışma oturumunda, ilgili CraftStation örneğinin belleğinde tutulur. Panel kapatmak veya tezgâhı geçici kapatıp açmak rezervleri silmez; tezgâh devre dışı kalırsa süren üretim durur ve rezervler korunur. Sahne boşaltma, nesneyi silme, script domain reload ve oyundan çıkış sonrasında rezervlerin korunması desteklenmez. Bunlar için ileride gerçek kayıt sistemiyle entegrasyon gerekir; mevcut yapı kayıt sistemi gibi kullanılmamalıdır.

Fiziksel klavye/fare kontrolü kullanıcıda kalır. Arka plan testleri mevcut E/F işlem metotlarını ve UI buton olaylarını çağırır; fiziksel tuş basışlarının ayrıca kullanıcı tarafından denenmesi gerekir.

## Doğrulama sonuçları

Unity Play Mode arka plan entegrasyon testlerinde ortak E hedef seçimi/panel açma, gerçek Üret ve Malzeme Ekle butonları, açılış karesinde E'nin üretim başlatmaması, eksik malzeme engeli, panel kapatıp açınca rezerv koruma ve üretim spam engeli doğrulandı. Süreli üretimde 2 ayrı Lumber ve ardından 3 ayrı Pole üretildi; her biri mevcut F toplama işleminden geçirildi.

Çoklu stack çıkarımı, üç farklı malzemeli tarif, iki tezgâhın bağımsız rezervleri, dolu envanterde atomik iade engeli, engellenen tarif değişimi ve tekil/toplu geri alma geçti. Sınırlı zeminde kısmi spawn başarısızlığı simüle edildi: görünür kısmi ürün kalmadı, malzemeler korunup iade edilebildi. Sıfır süreli 100 ardışık üretimde her seferinde 2 ürün oluştu, UI RectTransform sayısı 41'den 41'e sabit kaldı.

Eksik prefab/ItemData/OutputPoint, yinelenen malzeme, sıfır çıktı ve negatif malzeme miktarı reddedildi. Son sahne doğrulamasında eksik script/kırık prefab bulunmadı; ortak CraftingUI sayısı 1 idi. Derleme başarılı. Console'da oyun kodu hatası yoktu; test sırasında bağlantı kesilmesine ait tek bir MCP WebSocket uyarısı kaldı. Fiziksel E/F/G/TAB ve ESC basışları kullanıcı kontrolü nedeniyle gönderilmedi; mevcut giriş engelleme yolları kod üzerinden incelendi.

Makine tarafından üretilen test raporu: `Library/UnityMcpSetup/Crafting/test-results.json` (Library geçici/verisyonlanmayan klasördür).

## Sadeleştirilmiş UI doğrulaması (5 Kasım 2026)

Tarifi Seç / Ekle / Geri Al / Üret akışı mevcut panelde uygulanır. E yalnızca tezgâha erişir; eski HandleCraftInput artık üretim başlatmaz. Tarif listesinde yalnızca ürün adı, miktarı ve ikon bulunur. İki kaydırılabilir liste ve çözünürlüğe uyan pencere korunur. Başlangıçta malzeme butonları gizlidir; Üret yalnızca tam rezervde görünür. Inventory ve WorldItem transfer/spawn altyapısı değiştirilmedi.

Play Mode testleri: stoksuz/kısmi yükleme, tümünü geri alma, dolu envanterde güvenli iade engeli, yüklü tarif değişim engeli (UI ve core), kapat/aç rezerv koruma, buton spam engeli, 2 Lumber üretip toplama ve 3 Pole üretimi geçti. Akış testinde Console hata/uyarı: 0. Son yazı boyutu derlemesinde 5 mevcut kullanılmayan alan uyarısı ve 1 MCP bağlantı uyarısı görüldü; derleme hatası yok. Başka tezgâha geçiş ve kapat butonu ayrıca doğrulandı. Fiziksel ESC girdisi gönderilmedi; mevcut ESC kapatma yolu korundu. Ayrıntı: Library/UnityMcpSetup/Crafting/simplified-ui-tests.json.
