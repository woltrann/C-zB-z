# Yeni karakter entegrasyonu

Sahnedeki Character kökü, PlayerMovement / PlayerInteraction / PlayerToolController / Inventory bileşenleri, Rigidbody, CapsuleCollider ve mevcut Animator Controller korunur. Kaynak yeni karakter FBX'i statik olduğundan dört mesh için skin ağırlıkları ve yeni orana uygun Humanoid avatar üretildi.

Character/YeniKarakterVisual altında dört SkinnedMeshRenderer bulunur: gövde, kemer, şort ve çanta. Gövde/şort ağırlıkları mevcut gövde referansından aktarıldı; stilize baş boyun/baş kemiklerine, kemer Hips'e ve çanta Chest'e bağlandı. Önceki iskeletin Transform nesneleri korunup eklem konumları yeni oranlara uyarlandı; aynı el/kemer socket referansları kullanılmaya devam eder. YeniKarakterAvatar.asset bu oranları Humanoid kliplere bağlar. Animator Controller değişmedi: Assets/AA Character/Animator/1UnityChanActionCheck.controller.

Üretilen dört mesh ve avatar bu klasördedir. Kaynak FBX'in Read/Write ayarı mesh verisini okuyabilmek için açıldı; kaynak FBX'e animasyon klibi eklenmedi. Sahne eski karakterin renderer'larını kapatır; önceki mesh veya iskelet nesneleri silinmedi. Eski görünen çanta da çift model oluşmaması için renderer seviyesinde gizlendi. BagData, slot kapasitesi ve ekipman mekanikleri değiştirilmedi.

Bekleme, yürüme ve koşma Play Mode'da kontrol edildi. Dört mesh için ağırlık toplamları 1; 39 Humanoid eşleşme aynı Armature altındadır, SkinnedMeshRenderer bone dizileri 46 Transform kullanır. El socket'ine ekipman takma, dolu kemerle değiştirme ve bırakma işlem metotları arka planda test edildi. Klavye/fare girdisi gönderilmedi. Test kamerası ve test konumu yalnızca Play Mode'da değiştirildi; durdurulunca sahnedeki oyuncu konumu korundu.

Bu otomatik bir skin aktarımıdır; ileride farklı animasyonlar veya büyük model değişimleri eklenirse eklem ağırlıklarının yeniden gözden geçirilmesi gerekebilir. Kaynak mesh güncellenirse üretilen skin meshleri kendiliğinden yenilenmez.

Sahne kullanıcı talimatı gereği kaydedilmedi. Yeni karakter, kemik dönüşümleri ve Animator avatar bağlantısını korumak için GameScene'i kaydedin. Mesh/Avatar assetleri diske oluşturuldu. İlk oluşturma ve rig düzeltme komutları Library/UnityMcpSetup/skin_new_character.json ve refine_character_rig.json içinde, ekran görüntüleri ve ekipman test raporu Library/UnityMcpSetup/NewCharacter altında bulunur (Library geçici klasördür).
