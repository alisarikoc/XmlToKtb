using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb.Las
{
    public enum LasKesitTipi
    {
        /// <summary>Soldan sağa, her nokta aralığında en düşük kotlu nokta (arazi/zemin kesiti).</summary>
        Zemin,
        /// <summary>Eksen çevresinde dönerek alınan kapalı çevre (tünel iç yüzeyi).</summary>
        Tunel
    }

    public class LasEnkesitAyarlari
    {
        public double BaslangicKm, BitisKm;
        /// <summary>Kesitler arası kilometre aralığı (m).</summary>
        public double Aralik = 1;
        /// <summary>Eksenin sağına ve soluna alınacak genişlik (m).</summary>
        public double Genislik = 10;
        /// <summary>Kesit düzleminin önünden ve arkasından toplam ne kadarlık dilimdeki noktaların alınacağı (m).</summary>
        public double DilimKalinligi = 0.10;
        /// <summary>Kesit üzerinde ardışık noktalar arasındaki yaklaşık mesafe (m).</summary>
        public double NoktaAraligi = 0.10;
        public LasKesitTipi Tip = LasKesitTipi.Zemin;
    }

    /// <summary>Nokta bulutundan, bir güzergahın verilen kilometrelerinde eksene dik enkesitler çıkarır.</summary>
    public static class LasEnkesit
    {
        /// <summary>
        /// Enkesitleri KSE dosyası olarak üretir. Kesit noktalarında mesafe eksenin sağında artı, solunda eksidir;
        /// kot nokta bulutundaki kottur. İçinde yeterli nokta bulunmayan kilometreler atlanır ve
        /// <paramref name="bosKesit"/> ile sayılır.
        /// </summary>
        public static KseDosyasi Olustur(LasBulutu bulut, GuzergahNesnesi guzergah, LasEnkesitAyarlari ayar, bool[] sinifGorunur, Action<double> ilerleme, out int bosKesit)
        {
            bosKesit = 0;
            var dosya = new KseDosyasi();

            // Noktalar bir kez ızgara hücrelerine dağıtılır; her kesit yalnızca kendi şeridindeki hücrelere bakar.
            double minX = bulut.MinX - bulut.OrijinX, minY = bulut.MinY - bulut.OrijinY;
            double genislik = bulut.MaxX - bulut.MinX, yukseklik = bulut.MaxY - bulut.MinY;
            var hucre = Math.Max(Math.Max(0.5, ayar.DilimKalinligi), Math.Sqrt(genislik * yukseklik / 4e6));
            int nx = (int)(genislik / hucre) + 1, ny = (int)(yukseklik / hucre) + 1;

            var hucreBasi = new int[nx * ny + 1];
            var noktaHucresi = new int[bulut.Sayi];
            for (var i = 0; i < bulut.Sayi; i++)
            {
                if (!sinifGorunur[bulut.Sinif[i]])
                {
                    noktaHucresi[i] = -1;
                    continue;
                }
                var cx = Math.Min(nx - 1, Math.Max(0, (int)((bulut.X[i] - minX) / hucre)));
                var cy = Math.Min(ny - 1, Math.Max(0, (int)((bulut.Y[i] - minY) / hucre)));
                noktaHucresi[i] = cy * nx + cx;
                hucreBasi[cy * nx + cx + 1]++;
            }
            for (var c = 0; c < nx * ny; c++)
                hucreBasi[c + 1] += hucreBasi[c];
            var sirali = new int[hucreBasi[nx * ny]];
            var yazilacakYer = (int[])hucreBasi.Clone();
            for (var i = 0; i < bulut.Sayi; i++)
            {
                if (noktaHucresi[i] >= 0)
                    sirali[yazilacakYer[noktaHucresi[i]]++] = i;
            }

            var damga = new int[nx * ny];
            var damgaNo = 0;
            var yariDilim = ayar.DilimKalinligi / 2;
            var mesafeler = new List<double>();
            var kotlar = new List<double>();

            var kesitSayisi = (int)Math.Floor((ayar.BitisKm - ayar.BaslangicKm) / ayar.Aralik + 1e-9) + 1;
            for (var k = 0; k < kesitSayisi; k++)
            {
                var km = ayar.BaslangicKm + k * ayar.Aralik;
                ilerleme?.Invoke((double)k / kesitSayisi);
                if (!guzergah.KonumAl(km, out var x, out var y, out var aci))
                {
                    bosKesit++;
                    continue;
                }

                x -= bulut.OrijinX;
                y -= bulut.OrijinY;
                // Gidiş yönü (t) ve gidişe göre sağ taraf (s).
                double tx = Math.Cos(aci), ty = Math.Sin(aci), sx = ty, sy = -tx;

                mesafeler.Clear();
                kotlar.Clear();
                damgaNo++;
                for (var s = -ayar.Genislik; s <= ayar.Genislik + hucre / 2; s += hucre / 2)
                {
                    var ox = (int)Math.Floor((x + sx * s - minX) / hucre);
                    var oy = (int)Math.Floor((y + sy * s - minY) / hucre);
                    for (var cy = oy - 1; cy <= oy + 1; cy++)
                    {
                        if (cy < 0 || cy >= ny)
                            continue;
                        for (var cx = ox - 1; cx <= ox + 1; cx++)
                        {
                            if (cx < 0 || cx >= nx)
                                continue;
                            var c = cy * nx + cx;
                            if (damga[c] == damgaNo)
                                continue;
                            damga[c] = damgaNo;

                            for (var j = hucreBasi[c]; j < hucreBasi[c + 1]; j++)
                            {
                                var i = sirali[j];
                                double dx = bulut.X[i] - x, dy = bulut.Y[i] - y;
                                if (Math.Abs(dx * tx + dy * ty) > yariDilim)
                                    continue;
                                var mesafe = dx * sx + dy * sy;
                                if (Math.Abs(mesafe) > ayar.Genislik)
                                    continue;
                                mesafeler.Add(mesafe);
                                kotlar.Add(bulut.Z[i]);
                            }
                        }
                    }
                }

                var noktalar = ayar.Tip == LasKesitTipi.Tunel
                    ? TunelKesiti(mesafeler, kotlar, ayar.NoktaAraligi)
                    : ZeminKesiti(mesafeler, kotlar, ayar.Genislik, ayar.NoktaAraligi);
                if (noktalar.Count < 2)
                {
                    bosKesit++;
                    continue;
                }

                var no = (dosya.Kesitler.Count + 1).ToString(CultureInfo.InvariantCulture);
                var kmMetni = km.ToString("0.000", CultureInfo.InvariantCulture);
                dosya.Kesitler.Add(new KseKesit { No = no, Kilometre = kmMetni, Km = km, Aciklama = "", Baslik = no + "  " + kmMetni, Noktalar = noktalar });
            }

            ilerleme?.Invoke(1);
            return dosya;
        }

        private static KseNokta Nokta(double mesafe, double kot)
        {
            return new KseNokta { Mesafe = Math.Round(mesafe, 3), Kot = Math.Round(kot, 3), Kod = "" };
        }

        // Soldan sağa, her nokta aralığındaki en düşük kotlu nokta: bitki, tel gibi zeminin üstündekiler elenir.
        private static List<KseNokta> ZeminKesiti(List<double> mesafeler, List<double> kotlar, double genislik, double aralik)
        {
            var dilimSayisi = (int)Math.Ceiling(2 * genislik / aralik) + 1;
            var secilen = new int[dilimSayisi];
            for (var d = 0; d < dilimSayisi; d++)
                secilen[d] = -1;

            for (var i = 0; i < mesafeler.Count; i++)
            {
                var d = Math.Min(dilimSayisi - 1, Math.Max(0, (int)((mesafeler[i] + genislik) / aralik)));
                if (secilen[d] < 0 || kotlar[i] < kotlar[secilen[d]])
                    secilen[d] = i;
            }

            var sonuc = new List<KseNokta>();
            foreach (var i in secilen)
            {
                if (i >= 0)
                    sonuc.Add(Nokta(mesafeler[i], kotlar[i]));
            }
            return sonuc;
        }

        // Dilimdeki noktalar, eksenin üstündeki bir merkez etrafında açıya göre gruplanır ve her açı aralığından
        // merkeze uzaklığı ortanca olan nokta alınır. Çevre tabandan başlar; sol duvar, tavan ve sağ duvardan geçip
        // yine tabanda biter.
        private static List<KseNokta> TunelKesiti(List<double> mesafeler, List<double> kotlar, double aralik)
        {
            var sonuc = new List<KseNokta>();
            var sayi = mesafeler.Count;
            if (sayi < 8)
                return sonuc;

            // Merkez kotu: birkaç uç nokta bozmasın diye kotların %2 ile %98'inin ortası.
            var siraliKotlar = kotlar.ToArray();
            Array.Sort(siraliKotlar);
            var merkezKot = (siraliKotlar[(int)(sayi * 0.02)] + siraliKotlar[Math.Min(sayi - 1, (int)(sayi * 0.98))]) / 2;

            var yaricaplar = new double[sayi];
            var acilar = new double[sayi];
            for (var i = 0; i < sayi; i++)
            {
                double m = mesafeler[i], h = kotlar[i] - merkezKot;
                yaricaplar[i] = Math.Sqrt(m * m + h * h);
                // Açı aşağıdan başlar ve sola doğru artar (0: taban, 90°: sol, 180°: tavan, 270°: sağ).
                var aci = Math.Atan2(-m, -h);
                acilar[i] = aci < 0 ? aci + 2 * Math.PI : aci;
            }

            var siraliYaricaplar = (double[])yaricaplar.Clone();
            Array.Sort(siraliYaricaplar);
            var ortancaYaricap = siraliYaricaplar[sayi / 2];
            var dilimSayisi = Math.Max(72, Math.Min(2880, (int)(2 * Math.PI * ortancaYaricap / aralik)));

            var siralar = new int[sayi];
            var dilimler = new int[sayi];
            for (var i = 0; i < sayi; i++)
            {
                dilimler[i] = Math.Min(dilimSayisi - 1, (int)(acilar[i] / (2 * Math.PI) * dilimSayisi));
                siralar[i] = i;
            }
            Array.Sort(siralar, (a, b) => dilimler[a] != dilimler[b] ? dilimler[a].CompareTo(dilimler[b]) : yaricaplar[a].CompareTo(yaricaplar[b]));

            var bas = 0;
            while (bas < sayi)
            {
                var son = bas;
                while (son + 1 < sayi && dilimler[siralar[son + 1]] == dilimler[siralar[bas]])
                    son++;
                var orta = siralar[(bas + son) / 2];
                sonuc.Add(Nokta(mesafeler[orta], kotlar[orta]));
                bas = son + 1;
            }
            return sonuc;
        }
    }

    /// <summary>Enkesit alma ayarlarının sorulduğu pencere.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class LasEnkesitForm : Form
    {
        private readonly LasEnkesitAyarlari _ayarlar;
        private readonly double _guzergahBasKm, _guzergahBitKm;
        private readonly TextBox _basKutu, _bitKutu, _aralikKutu, _genislikKutu, _dilimKutu, _noktaKutu;
        private readonly ComboBox _tipKutu;

        public LasEnkesitForm(LasEnkesitAyarlari ayarlar, double guzergahBasKm, double guzergahBitKm)
        {
            _ayarlar = ayarlar;
            _guzergahBasKm = guzergahBasKm;
            _guzergahBitKm = guzergahBitKm;

            Text = "Enkesit Al";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 290);

            var y = 14;
            TextBox Satir(string etiket, double deger)
            {
                Controls.Add(new Label { Text = etiket, Location = new Point(12, y + 3), AutoSize = true });
                var kutu = new TextBox { Text = deger.ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(230, y), Size = new Size(136, 20) };
                Controls.Add(kutu);
                y += 28;
                return kutu;
            }

            _basKutu = Satir("Başlangıç kilometresi (m):", ayarlar.BaslangicKm);
            _bitKutu = Satir("Bitiş kilometresi (m):", ayarlar.BitisKm);
            _aralikKutu = Satir("Kilometre aralığı (m):", ayarlar.Aralik);
            _genislikKutu = Satir("Eksenin sağına/soluna genişlik (m):", ayarlar.Genislik);
            _dilimKutu = Satir("Dilim kalınlığı (m):", ayarlar.DilimKalinligi);
            _noktaKutu = Satir("Kesitteki nokta aralığı (m):", ayarlar.NoktaAraligi);

            Controls.Add(new Label { Text = "Kesit tipi:", Location = new Point(12, y + 3), AutoSize = true });
            _tipKutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(130, y), Size = new Size(236, 21) };
            _tipKutu.Items.AddRange(new object[] { "Zemin (en düşük kotlar, soldan sağa)", "Tünel (çevre profili)" });
            _tipKutu.SelectedIndex = (int)ayarlar.Tip;
            Controls.Add(_tipKutu);
            y += 32;

            Controls.Add(new Label
            {
                Text = "Güzergah: " + GuzergahNesnesi.KmMetni(guzergahBasKm) + " - " + GuzergahNesnesi.KmMetni(guzergahBitKm),
                Location = new Point(12, y),
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            });

            var tamam = new Button { Text = "Kaydet (KSE)...", Location = new Point(176, 252), Size = new Size(110, 26) };
            var iptal = new Button { Text = "İptal", Location = new Point(292, 252), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
            tamam.Click += (s, e) =>
            {
                if (AyarlariAl())
                    DialogResult = DialogResult.OK;
            };
            Controls.Add(tamam);
            Controls.Add(iptal);
            AcceptButton = tamam;
            CancelButton = iptal;
        }

        private bool AyarlariAl()
        {
            string hata = null;
            if (!NcnDosyasi.SayiOku(_basKutu.Text, out var bas) || !NcnDosyasi.SayiOku(_bitKutu.Text, out var bit) || bit < bas)
                hata = "Başlangıç ve bitiş kilometresi sayı olmalı, bitiş başlangıçtan küçük olmamalıdır.";
            else if (bit < _guzergahBasKm || bas > _guzergahBitKm)
                hata = "Verilen kilometre aralığı güzergahın dışında.";
            else if (!NcnDosyasi.SayiOku(_aralikKutu.Text, out var aralik) || aralik <= 0)
                hata = "Kilometre aralığı sıfırdan büyük olmalıdır.";
            else if ((bit - bas) / aralik > 200000)
                hata = "Bu aralıkla çok fazla kesit oluşuyor; aralığı büyütün.";
            else if (!NcnDosyasi.SayiOku(_genislikKutu.Text, out var genislik) || genislik <= 0)
                hata = "Genişlik sıfırdan büyük olmalıdır.";
            else if (!NcnDosyasi.SayiOku(_dilimKutu.Text, out var dilim) || dilim <= 0)
                hata = "Dilim kalınlığı sıfırdan büyük olmalıdır.";
            else if (!NcnDosyasi.SayiOku(_noktaKutu.Text, out var nokta) || nokta < 0.001)
                hata = "Nokta aralığı en az 0.001 m olmalıdır.";
            else
            {
                _ayarlar.BaslangicKm = bas;
                _ayarlar.BitisKm = bit;
                _ayarlar.Aralik = aralik;
                _ayarlar.Genislik = genislik;
                _ayarlar.DilimKalinligi = dilim;
                _ayarlar.NoktaAraligi = nokta;
                _ayarlar.Tip = (LasKesitTipi)_tipKutu.SelectedIndex;
                return true;
            }

            MessageBox.Show(hata, "Enkesit Al", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }
}
