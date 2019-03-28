using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMailRuParser.BLL.Contracts;
using AutoMailRuParser.Entities;
using HtmlAgilityPack;

namespace AutoMailRuParser.BLL
{
    public class CarsLogic : ICarsLogic
    {
        private static readonly string urlMain = "https://auto.mail.ru/";
        private static readonly string urlCatalog = "https://auto.mail.ru/catalog/search/?page=";
        private static readonly string firstCatalogPage = "1";
        private static readonly int firstCatalogPageNum = 1;

        public IEnumerable<Car> GetAllCarsInfo()
        {
            int lastCatalogPageNum = GetLastCatalogPage();

            List<Car> result = new List<Car>();

            for (int i = firstCatalogPageNum; i <= lastCatalogPageNum; i++)
            {
                string url = urlCatalog + i.ToString();

                var htmlDocument = GetHtmlDocument(url);

                var catalogItems = GetItemsFromCatalog(htmlDocument);

                foreach (var catalogItem in catalogItems)
                {
                    string link = GetCatalogItemLink(catalogItem);

                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        url = Path.Combine(urlMain, link);

                        htmlDocument = GetHtmlDocument(url);

                        var modificationItems = GetModificationListItems(htmlDocument);

                        foreach (var modificationItem in modificationItems)
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

        private static HtmlDocument GetHtmlDocument(string url)
        {
            var web = new HtmlWeb();
            return web.Load(url);
        }

        private static int GetLastCatalogPage()
        {
            string url = Path.Combine(urlCatalog, firstCatalogPage);

            var htmlDocument = GetHtmlDocument(url);

            var linkNode = htmlDocument.DocumentNode.Descendants("a")
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
            var linkNode = node.Descendants("a")
                .Where(link => link.GetAttributeValue("class", "")
                .Equals("hdr__text"))
                .FirstOrDefault();

            if (linkNode != null)
            {
                resultLink = linkNode.GetAttributeValue("href", "").Substring(1);
            }

            return resultLink;
        }

        private static IEnumerable<HtmlNode> GetModificationListItems(HtmlDocument doc)
        {
            return doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("cell padding_10 padding_bottom_5"))
                .ToArray();
        }

        private static string GetModificationPageLink(HtmlNode node)
        {
            string resultLink = string.Empty;

            var linkNode = node.Descendants("a")
                .Where(link => link.GetAttributeValue("class", "")
                .Equals("text text_bold_medium"))
                .FirstOrDefault();

            if (linkNode != null)
            {
                resultLink = linkNode.GetAttributeValue("href", "").Substring(1);
            }

            return resultLink;
        }

        private static Car GetCarInfo(HtmlDocument doc)
        {
            Car resultCar = new Car();

            HtmlNode[] nodes = doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("link__text"))
                .ToArray();

            resultCar.Brand = nodes[1].InnerText;

            resultCar.Model = nodes[2].InnerText;

            var headerNode = doc.DocumentNode.Descendants("h1")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("hdr hdr_bold_huge hdr_color_white hdr_collapse"))
                .FirstOrDefault();

            if (headerNode != null)
            {
                var yearsNode = headerNode.Descendants("span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("hdr__ending color_gray"))
                    .FirstOrDefault();

                if (yearsNode != null)
                {
                    resultCar.ProductionYears = yearsNode.InnerText.Replace("&ndash;", "-");
                }
            }

            var priceNode = doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("text text_slab_medium margin_right_10"))
                .FirstOrDefault();

            if (priceNode != null)
            {
                resultCar.Price = priceNode.InnerText.Replace("&nbsp;", " ");
            }

            nodes = doc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("js-specs-content js-specs-content_active"))
                .ToArray();

            var modificationNode = nodes[0].Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("text text_bold_medium"))
                .FirstOrDefault();

            if (modificationNode != null)
            {
                resultCar.Modification = modificationNode.InnerText;
            }

            var descriptionNode = nodes[0].Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("padding_bottom_10"))
                .FirstOrDefault();

            if (descriptionNode != null)
            {
                resultCar.Description = descriptionNode.InnerText.Replace("&nbsp;", " ");
            }

            var specNodes = nodes[1]
                .ChildNodes
                .ToArray();

            for (int i = 0; i < specNodes.Length; i++)
            {
                if (i < specNodes.Length && specNodes[i].InnerText == "Двигатель")
                {
                    resultCar.EngineSpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Динамические характеристики")
                {
                    resultCar.DynamicSpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Трансмиссия")
                {
                    resultCar.TransmissionSpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Ходовая часть")
                {
                    resultCar.ChassisSpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Кузов")
                {
                    resultCar.BodySpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Рулевое управление")
                {
                    resultCar.SteeringSpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Размеры, масса, объемы")
                {
                    resultCar.DimensionsSpec = GetSpecs(ref i, specNodes);
                }

                if (i < specNodes.Length && specNodes[i].InnerText == "Прочее")
                {
                    resultCar.OtherSpec = GetSpecs(ref i, specNodes);
                }
            }

            return resultCar;
        }

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
