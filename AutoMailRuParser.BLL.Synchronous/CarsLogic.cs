using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using AutoMailRuParser.Entities;

namespace AutoMailRuParser.BLL.Synchronous
{
    /// <summary>
    /// Класс с логикой по сбору информации по машинам
    /// </summary>
    public class CarsLogic
    {
        private static readonly string urlMain = "https://auto.mail.ru/";
        private static readonly string urlCatalog = "https://auto.mail.ru/catalog/search/?page=";
        private static readonly string firstCatalogPage = "1";
        private static readonly int firstCatalogPageNum = 1;

        /// <summary>
        /// Единственный публичный метод, внутри которого запускаются части алгоритма.
        /// </summary>
        /// <returns>
        /// Возвращает перечисление машин.
        /// </returns>
        public IEnumerable<Car> GetAllCarsInfo()
        {
            int lastCatalogPageNum = GetLastCatalogPage();

            List<Car> result = new List<Car>();

            for (int i = firstCatalogPageNum; i <= lastCatalogPageNum; i++)
            {
                string url = urlCatalog + i.ToString();

                HtmlDocument htmlDocument = GetHtmlDocument(url);

                IEnumerable<HtmlNode> catalogItems = GetItemsFromCatalog(htmlDocument);

                foreach (HtmlNode catalogItem in catalogItems)
                {
                    string link = GetCatalogItemLink(catalogItem);

                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        url = Path.Combine(urlMain, link);

                        htmlDocument = GetHtmlDocument(url);

                        IEnumerable<HtmlNode> modificationItems = GetModificationListItems(htmlDocument);

                        foreach (HtmlNode modificationItem in modificationItems)
                        {
                            link = GetModificationPageLink(modificationItem);

                            if (!string.IsNullOrWhiteSpace(link))
                            {
                                url = Path.Combine(urlMain, link);

                                htmlDocument = GetHtmlDocument(url);

                                Car car = GetCarInfo(htmlDocument);

                                result.Add(car);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Метод для получения HTML документа
        /// </summary>
        /// <param name="url">
        /// Путь к странице
        /// </param>
        /// <returns>
        /// HTML документ
        /// </returns>
        private static HtmlDocument GetHtmlDocument(string url)
        {
            HtmlWeb web = new HtmlWeb();
            return web.Load(url);
        }

        private static int GetLastCatalogPage()
        {
            string url = Path.Combine(urlCatalog, firstCatalogPage);

            HtmlDocument htmlDocument = GetHtmlDocument(url);

            HtmlNode linkNode = htmlDocument.DocumentNode.Descendants("a")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("paging__link paging__link_last js-paging__page"))
                .FirstOrDefault();

            if (linkNode != null)
            {
                if (int.TryParse(linkNode.InnerText, out int num))
                {
                    return num;
                }
            }

            return firstCatalogPageNum;
        }

        /// <summary>
        /// Сбор нод с ссылками на страницы моделей машин со страницы поиска
        /// </summary>
        /// <param name="doc">
        /// HTML документ со страницей поиска
        /// </param>
        /// <returns>
        /// Коллекцию нод с ссылками на страницы моделей
        /// </returns>
        private static IEnumerable<HtmlNode> GetItemsFromCatalog(HtmlDocument doc)
        {
            return doc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("p-search__item js-module link-hdr"))
                .ToArray();
        }

        private static string GetCatalogItemLink(HtmlNode node)
        {
            string resultLink = string.Empty;
            HtmlNode linkNode = node.Descendants("a")
                .Where(link => link.GetAttributeValue("class", "")
                .Equals("hdr__text"))
                .FirstOrDefault();

            if (linkNode != null)
            {
                resultLink = linkNode.GetAttributeValue("href", "").Substring(1);
            }

            return resultLink;
        }

        /// <summary>
        /// Сбор нод с ссылками на страницы модификаций модели машины
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static IEnumerable<HtmlNode> GetModificationListItems(HtmlDocument doc)
        {
            return doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("cell padding_10 padding_bottom_5"))
                .ToArray();
        }

        /// <summary>
        /// Метод для получения ссылки на страницу модификации модели машины
        /// </summary>
        /// <param name="node">
        /// Нода с ссылкой на страницу модификации модели машины
        /// </param>
        /// <returns></returns>
        private static string GetModificationPageLink(HtmlNode node)
        {
            string resultLink = string.Empty;

            HtmlNode linkNode = node.Descendants("a")
                .Where(link => link.GetAttributeValue("class", "")
                .Equals("text text_bold_medium"))
                .FirstOrDefault();

            if (linkNode != null)
            {
                resultLink = linkNode.GetAttributeValue("href", "").Substring(1);
            }

            return resultLink;
        }

        /// <summary>
        /// Метод для получения информации по модификации модли машины
        /// </summary>
        /// <param name="doc">
        /// HTML документ с информацией по модификации модели машины
        /// </param>
        /// <returns>
        /// Объект машины
        /// </returns>
        private static Car GetCarInfo(HtmlDocument doc)
        {
            Car resultCar = new Car();

            HtmlNode[] nodes = doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("link__text"))
                .ToArray();

            if (nodes.Length >= 3)
            {
                resultCar.Brand = nodes[1].InnerText;

                resultCar.Model = nodes[2].InnerText;
            }
            else
            {
                resultCar.Brand = string.Empty;

                resultCar.Model = string.Empty;
            }

            HtmlNode headerNode = doc.DocumentNode.Descendants("h1")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("hdr hdr_bold_huge hdr_color_white hdr_collapse"))
                .FirstOrDefault();

            if (headerNode != null)
            {
                HtmlNode yearsNode = headerNode.Descendants("span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("hdr__ending color_gray"))
                    .FirstOrDefault();

                if (yearsNode != null)
                {
                    resultCar.ProductionYears = yearsNode.InnerText.Replace("&ndash;", "-");
                }
                else
                {
                    resultCar.ProductionYears = string.Empty;
                }
            }
            else
            {
                resultCar.ProductionYears = string.Empty;
            }

            HtmlNode priceNode = doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("text text_slab_medium margin_right_10"))
                .FirstOrDefault();

            if (priceNode != null)
            {
                resultCar.Price = priceNode.InnerText.Replace("&nbsp;", " ");
            }
            else
            {
                resultCar.Price = string.Empty;
            }

            nodes = doc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("js-specs-content js-specs-content_active"))
                .ToArray();

            HtmlNode modificationNode = nodes[0].Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("text text_bold_medium"))
                .FirstOrDefault();

            if (modificationNode != null)
            {
                resultCar.Modification = modificationNode.InnerText;
            }
            else
            {
                resultCar.Modification = string.Empty;
            }

            HtmlNode descriptionNode = nodes[0].Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("padding_bottom_10"))
                .FirstOrDefault();

            if (descriptionNode != null)
            {
                resultCar.Description = descriptionNode.InnerText.Replace("&nbsp;", " ");
            }
            else
            {
                resultCar.Description = string.Empty;
            }

            HtmlNode[] specNodes = nodes[1]
                .ChildNodes
                .ToArray();

            for (int i = 0; i < specNodes.Length; i++)
            {
                if (i < specNodes.Length && specNodes[i].InnerText == "Двигатель")
                {
                    resultCar.EngineSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.EngineSpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Динамические характеристики")
                {
                    resultCar.DynamicSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.DynamicSpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Трансмиссия")
                {
                    resultCar.TransmissionSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.TransmissionSpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Ходовая часть")
                {
                    resultCar.ChassisSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.ChassisSpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Кузов")
                {
                    resultCar.BodySpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.BodySpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Рулевое управление")
                {
                    resultCar.SteeringSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.SteeringSpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Размеры, масса, объемы")
                {
                    resultCar.DimensionsSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.DimensionsSpec = new Dictionary<string, string>();
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Прочее")
                {
                    resultCar.OtherSpec = GetSpecs(ref i, specNodes);
                }
                else
                {
                    resultCar.OtherSpec = new Dictionary<string, string>();
                }
            }

            return resultCar;
        }

        /// <summary>
        /// Метод для сбора данных с блока информации по модификации модели машины (вспомогательный к GetCarInfo)
        /// </summary>
        /// <param name="i">
        /// счетчик цикла
        /// </param>
        /// <param name="specNodes">
        /// массив нод с данными
        /// </param>
        /// <returns>
        /// коллекция данных
        /// </returns>
        private static Dictionary<string, string> GetSpecs(ref int i, HtmlNode[] specNodes)
        {
            Dictionary<string, string> resultDicSpec = new Dictionary<string, string>();
            HtmlNode[] specPair = null;
            i++;
            while (specNodes[i].GetAttributeValue("class", "").Equals("cols cols_percent"))
            {
                specPair = specNodes[i].Descendants("div")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("cols__inner"))
                    .ToArray();

                if (specPair != null && specPair.Length == 2)
                {
                    resultDicSpec.Add(specPair[0].InnerText, specPair[1].InnerText);
                    specPair = null;
                }

                if (specNodes[i].NextSibling == null || !specNodes[i].NextSibling.GetAttributeValue("class", "").Equals("cols cols_percent"))
                {
                    i++;
                    break;
                }

                i++;
            }

            return resultDicSpec;
        }
    }
}
