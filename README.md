# Bilgi
Projenin amacı xml formatında line curve spiral bilgilerinden faydalanarak netcad uyumlu ktb dosyası oluşturmaktır.

YN2Xml.cs dosyasında xml formatına uygun serialize edilmiş bir sınıf var. Input verilecek xml dosya formatı güncelenirse bu
dosya güncellenmeli

XmlDeserializer herhangi tipte bir xml alarak deserialize eder.

XmlToKtbUI ekran kısmıdır. Görsel güncellemeler design tarafında yapılabilir.
View Code yaparsak göreceğimiz metot görevleri aşağıdaki gibidir.

openLandXmlButton_Click 
-Seç ve Kaydet butonuna basıldığında bu metot çalışır ve dosya input edilebilmesi için Dialog ekran gösterilir.
-Seçilen dosya için aşağıdaki metotlar çağırılarak dönüşümler hesaplamalar yapılır

ktbFileStringBuilder
-xml örneğini alır gerekli hesapları yaparak ktb formatına çevirir.

saveToKtb
-ktb formatında üretilen content'i alır ve YN2.KTB adıyla kullanıcıdan dosyanın kaydedileceği konumu ister.


# Başlarken
1.	Release mod'da build edilerek üretilen exe dosyası kodun bulunduğu yerden alınarak çalıştırılır.
2.	Windows bilgisayarda .net framework 4.6.1 kurulu olmalıdır.

# Build and Test
Test için bir proje yazılacak. Master her güncellendiğinde build edilmesi için CI tanımlandı.

# Contribute
Nesneye yönelik yaklaşımla düzenleyebilirsiniz.