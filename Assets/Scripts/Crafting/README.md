# CraftStation kullanımı

Üretim, rezerv, görsel yerleşimi ve istif ayarları mevcut CraftStation component'indedir. Ayrı CraftStationVisuals scripti ve component'i kaldırıldı. CraftOutputSpawner mevcut ürün oluşturma yardımcısı olarak kalır; yeni bir üretim sistemi eklenmedi.

## Inspector

- Recipes: ürün WorldItem prefabı, miktarı, süresi ve gerekli ItemData/miktar listesi. Tek tariflik malzeme kabul edilir; Ekle her basışta 1 adet aktarır ve gereken miktarda durur. Tüm girdiler tamamlanınca Üret görünür, yalnızca bir tarif üretilir.
- Input Point: ilk giriş görselinin prefab kökünü yerleştiren nokta. İkinci ve sonraki adetler noktanın yerel sağ yönüne (kırmızı X) eklenme sırasıyla dizilir. Model genişliği ve Input Gap birlikte hesaba katılır.
- Input Spacing: Input Point alanının hemen altındadır; varsayılan 0,5 dünya birimidir. İlk görsel noktada, sonraki görseller her defasında bu sabit mesafe kadar noktanın sağına dizilir. Model genişliği otomatik eklenmez; düzen kullanıcı tarafından ayarlanır.
- Giriş görselleri malzemenin mevcut ItemData.WorldPrefab alanından otomatik alınır. Input Visuals listesi yoktur. Görsel yalnızca mesh/materyal kopyasıdır; WorldItem veya Collider scriptleri çalıştırılmaz. Prefabın mevcut ölçeği kullanılır.
- Output Point: mevcut çıktı noktası; ürünler aynı X/Z merkezinde, bu noktadan başlayarak yukarı istiflenir. İlk ürünün altında ve ürünler arasında Stack Gap kullanılır. Yeni üretimler toplanmamış ürünlerin üstünden devam eder.
- Stack Gap: dikey istif boşluğu; mevcut ayar 0,05. Mesh ve BoxCollider boyutları hesaba katılır.
- Keep Stack Stable: açıkken ürün Rigidbody'leri kinematic tutulur; kapalıysa prefabın fizik davranışı devam eder.
- Output Visuals listesi yoktur. Çıktılar tarifteki Output World Prefab üzerinden, prefabın mevcut boyut ve Collider ayarlarıyla oluşturulur; ürün başına ayrıca görünüm tanımlanmaz.

Input Point boş bırakılırsa üretim çalışabilir; yalnızca giriş görselleri gösterilmez. Input Point atanmışsa malzemelerin mevcut ItemData.WorldPrefab alanları dolu olmalıdır. Giriş görselleri rezerv değişimlerinde güncellenir; Update taraması yapılmaz ve modeller yeniden kullanılır. Output Point'in kullanıcı tarafından belirlenen konumu korunur. Tezgâhın kendi geniş Collider'ı çıktı engeli sayılmaz; diğer engellerde kısmi ürünler temizlenir ve malzemeler korunur.

## Güvenlik ve yaşam döngüsü

Tarifin ihtiyacından fazlası yüklenemez. Tarif değişiminde önce eklenen malzemeler geri alınmalıdır. Panel kapatmak rezervleri silmez. Geri alma başarısızsa malzeme/görsel korunur. İptal üretimi durdurur; envanter doluysa rezervler tezgahta kalır, yer açılınca iade edilebilir. Ürünler mevcut WorldItem toplama davranışını korur. Kayıt/sahne geçişi kalıcılığı eklenmedi.

Sahne ve CuttingStation prefabındaki eski component ayarları CraftStation'a aktarıldı; script kaldırılmadan önce referanslar korundu. CraftStationVisuals dosyasının çalışma kopyası Library/UnityMcpSetup/RemovedCraftingScripts altında tutuldu. Sahne otomatik kaydedilmedi; kullanıcı kaydetmelidir.

## Doğrulama

İki Lumber gerektiren tarifle UI testinde her tıklamada 1 adet, InputPoint'teki ilk görsel, sağındaki ikinci görsel, tek üretim, OutputPoint üzerinde çakışmadan üç Pole istifi ve ayrı ayrı toplama doğrulandı. Rapor: Library/UnityMcpSetup/Crafting/two-lumber-single-script-tests.json. Diğer regresyon raporları single-script-crafting-tests.json ve single-script-crafting-safety-tests.json dosyalarındadır. Fiziksel tuş/fare girdisi gönderilmedi.

Son sadeleştirme testi: Inspector görsel listelerinin kaldırıldığı, ItemData prefabının kullanıldığı, varsayılan 0,5 aralık ve Inspector üzerinden 1,25 aralığın uygulanması, tek üretim ve üç Pole istifi/toplaması doğrulandı. Rapor: Library/UnityMcpSetup/Crafting/fixed-input-spacing-tests.json.
