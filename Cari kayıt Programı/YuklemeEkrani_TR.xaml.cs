﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class YuklemeEkrani_TR : Page
    {
        public YuklemeEkrani_TR()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "YuklemeEkrani_TR", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = StartDownload();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "YuklemeEkrani_TR", methodName: "Page_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private async Task StartDownload()
        {
            try
            {
                var releaseInfo = await Anasayfa.Check.GetLatestReleaseInfoAsync();

                string urlP = releaseInfo.downloadUrl;

                Assembly? assembly = Assembly.GetExecutingAssembly();
                Version? version = assembly.GetName().Version;
                string? versionString = $"{version.Major}.{version.Minor}.{version.Build}".Replace(".", "");

                int v = Convert.ToInt32(versionString);

                if (File.Exists(ConfigManager.ExecutableFileName))
                {
                    int productVersion;
                    try
                    {
                        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(ConfigManager.ExecutableFileName);
                        productVersion = Convert.ToInt32(fileInfo.ProductVersion.Replace(".", ""));

                        if (v < productVersion)
                        {
                            statusLabel.Content = "İndirme tamamlandı, dosya açılıyor...";
                            await Task.Delay(1000); // İşlemi tamamlamadan önce bekleme

                            Process.Start(ConfigManager.ExecutableFileName);
                        }
                    }
                    catch (Exception)
                    {
                        statusLabel.Content = "Dosya bozuk, tekrar indiriliyor...";
                        await Task.Delay(1000);
                        await DownloadAndStartFile(urlP);
                    }
                }
                else
                {
                    await DownloadAndStartFile(urlP);
                }

                Window mainWindow = Window.GetWindow(this);
                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "YuklemeEkrani_TR", methodName: "StartDownload()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private async Task DownloadAndStartFile(string urlP)
        {
            HttpClient client = new HttpClient();
            try
            {
                if (!Degiskenler.guncel)
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));

                    client.Timeout = TimeSpan.FromMinutes(10); // İstek zaman aşımı
                    using (var response = await client.GetAsync(urlP, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode(); // Başarılı yanıt mı diye kontrol

                        long? contentLength = response.Content.Headers.ContentLength;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var fileStream = new FileStream(ConfigManager.ExecutableFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                long totalBytes = contentLength ?? -1;
                                long totalBytesRead = 0;
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                                    totalBytesRead += bytesRead;

                                    // İlerleme hesapla ve güncelle
                                    int progressPercentage = (int)((float)totalBytesRead / totalBytes * 100);
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        progressBar.Value = progressPercentage;
                                        statusLabel.Content = $"İndiriliyor: {progressPercentage}%";
                                    });
                                }
                            }
                        }
                    }
                    statusLabel.Content = "İndirme tamamlandı, dosya açılıyor...";
                    await Task.Delay(1000); // İşlemi tamamlamadan önce bekleme

                    try
                    {
                        Process.Start(ConfigManager.ExecutableFileName);
                    }
                    catch (Exception ex)
                    {
                        statusLabel.Content = "Dosya çalıştırılamadı, tekrar indiriliyor...";
                        LogManager.LogError(ex, className: "YuklemeEkrani_TR", methodName: "DownloadAndStartFile() / File Open Process", stackTrace: ex.StackTrace);
                        await DownloadAndStartFile(urlP); // İndirme işlemini tekrar dene
                    }
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "YuklemeEkrani_TR", methodName: "DownloadAndStartFile()", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}
