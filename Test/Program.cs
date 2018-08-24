using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;
using System.Net;
using System.IO;
using System.Text;

class WebDonloader
{
    static void Main(string[] args)
    {
        Console.WriteLine("Запущено сканирование страниц раздела \"предстоящие ICO\" сайта icobench.com ...\n");

        //Создание служебной папки
        #region Create Directory

        string path = AppDomain.CurrentDomain.BaseDirectory + "Links";
        try
        {
            if (!Directory.Exists(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Создание папки Links вызвало ошибку: {0}", e.ToString());
            Console.WriteLine("\nДля завершения работы программы нажмите <Enter>");
            Console.ReadLine();
            return;
        }

        #endregion Create Directory

        int page = 1;
        string text = "";
        List<string> links = new List<string>();

        //Цикл прохода по всем страницам раздела на сайте
        while (true)
        {
            try
            {
                // Скачивание html-кода страницы
                text = DonloadPage(page);

                if (text == "")
                    throw new Exception("Не удалось скачать html-код страницы\n");

                // Поиск в скаченном тексте сообщения о завершении списка ICO
                if (CheckEnd(ref text))
                {
                    Console.WriteLine("\nСканирование завершено. Скачано страниц: {0}\n", page - 1);
                    break;
                }

                // Поиск ссылок в скаченном тексте
                if (!SearchLinks(ref text, ref links))
                {
                    throw new Exception("Поиск ссылок завершился с ошибкой");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка: " + e.Message);
                Console.WriteLine("Для завершения работы программы нажмите <Enter>");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Страница № {0} просканирована", page);
            page++;
        }

        //Поиск ранее уже обработанных ссылок
        #region Read Files

        List<string> oldLinks = new List<string>();

        try
        {
            string[] files = Directory.GetFiles(path);

            foreach(string file in files)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        oldLinks.Add(line);
                    }
                }
            }

            oldLinks = oldLinks.Distinct().ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine("The process opened files is failed: {0}", e.ToString());
            Console.WriteLine("Для завершения работы программы нажмите <Enter>");
            Console.ReadLine();
            return;
        }

        #endregion Read Files

        //Создание файла с новыми ссылками
        #region Create File

        DateTime localDate = DateTime.Now;
        string s = localDate.ToString();
        StringBuilder date = new StringBuilder(s);
        date.Replace(' ', '_');
        date.Replace(':', '-');

        try
        {
            using (StreamWriter sw = new StreamWriter((path + "\\" + date + ".txt")))
            {
                links = links.Distinct().ToList();

                var result = links.Except(oldLinks);

                foreach (var line in result)
                {
                    sw.WriteLine(line);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be write:");
            Console.WriteLine(e.Message);
            Console.WriteLine("\nДля завершения работы программы нажмите <Enter>");
            Console.ReadLine();
            return;
        }

        #endregion Create File

        Console.WriteLine("Файл со ссылками создан успешно.\n");
        Console.WriteLine("Для завершения работы программы нажмите <Enter>");
        Console.ReadLine();
        return;

    }

    private static bool SearchLinks(ref string text, ref List<string> links)
    {
        int amountLinks = 0;
        int startLink = 0;
        int endLink = 0;
        int lenghtLink = 0;
        string linkText = "";

        int beginSeachText = text.IndexOf("<a class=\"name\" href=\"/ico/", StringComparison.Ordinal);

        if (beginSeachText == -1)
            return false;

        while (beginSeachText != -1)
        {
            startLink = beginSeachText + 27;
            endLink = text.IndexOf("\"", startLink, StringComparison.Ordinal);

            if (endLink == -1)
                return false;

            lenghtLink = endLink - startLink;

            if (lenghtLink != 0)
            {
                linkText = text.Substring(startLink, lenghtLink);
                links.Add("https://icobench.com/ico/" + linkText);
                amountLinks++;            
            }
            beginSeachText = text.IndexOf("<a class=\"name\" href=\"/ico/", endLink, StringComparison.Ordinal);
        }

        if (amountLinks == 0)
            return false;

        return true;
    }

    private static bool CheckEnd(ref string text)
    {
        int index = text.IndexOf("We don't have the information about this ICO yet. You can publish it or search for something else.", StringComparison.Ordinal);

        return (index > -1) ? true : false;
    }

    private static string DonloadPage(int page)
    {
        WebRequest req = WebRequest.Create("https://icobench.com/icos?page=" + page + "&filterStatus=upcoming");

        HttpWebRequest request = (HttpWebRequest)req;
        request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.117 Safari/537.36";

        WebResponse resp;

        try
        {
            resp = request.GetResponse();
        }
        catch (System.Net.WebException)
        {
            Console.WriteLine("error downloading the page");
            Console.ReadLine();
            return "";
        }

        Stream istrm = resp.GetResponseStream();

        string text = "";

        int ch;

        for (int i = 1; ; i++)
        {
            ch = istrm.ReadByte();
            if (ch == -1) break;

            text += (char)ch;

        }
        resp.Close();

        return text;
    }
}
