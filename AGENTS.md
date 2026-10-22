# Proje Kuralları

1. Bu proje, Unity 6 ve URP kullanan 3D bir oyundur.
2. Mevcut çalışan sistemleri koru. Kullanıcı açıkça istemedikçe mevcut davranışları değiştirme.
3. Her görevden önce ilgili scriptleri, sınıfları ve bağımlılıklarını incele.
4. Yalnızca istenen görev için gerekli dosyaları değiştir. İlgisiz dosyalara dokunma.
5. Mevcut sınıf, metot, değişken ve public API isimlerini gerekmedikçe değiştirme.
6. Unity Inspector üzerinden atanmış `[SerializeField]` referanslarını koru.
7. Prefab, Scene, ScriptableObject ve `.meta` dosyalarını kullanıcı istemedikçe silme, yeniden oluşturma veya taşımaya çalışma.
8. Sahne ve prefab değişikliklerinde Unity MCP araçlarını tercih et.
9. Büyük veya riskli değişikliklerden önce yapılacak işlemleri açıkla ve kullanıcıdan onay iste.
10. Her görevden önce `git status` kontrolü yap. Kullanıcının mevcut değişikliklerini koru; `git reset`, `git clean` ve zorla checkout kullanma.
11. Kullanıcının izni olmadan git commit, push veya branch değiştirme.
12. Kullanıcı istemedikçe yeni Unity paketi veya bağımlılık yükleme.
13. Değişikliklerden sonra mümkünse Unity Console hatalarını kontrol et.
14. Görev sonunda değiştirilen dosyaları, yapılan işlemleri ve test sonuçlarını Türkçe bildir.
15. Kullanıcı istemedikçe sahneyi kaydetme.
16. Kodlarda mevcut mimariye ve Unity C# kodlama standartlarına uy.
17. Belirsiz durumlarda varsayım yaparak kritik değişiklikler gerçekleştirme; kullanıcıya sor.
