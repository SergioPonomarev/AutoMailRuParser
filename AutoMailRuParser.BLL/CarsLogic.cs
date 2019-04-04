using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMailRuParser.Entities;
using HtmlAgilityPack;

namespace AutoMailRuParser.BLL.AsyncWithRestrictions
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
        private static readonly int pagesRange = 20;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Car>> GetCars()
        {
            List<Car> cars = new List<Car>();

            int lastCatalogPageNum = await GetLastCatalogPageAsync();

            int firstPageInBlock = firstCatalogPageNum;
            int lastPageInBlock = pagesRange;

            int lastBlock = lastCatalogPageNum % pagesRange;
            int blocks = lastCatalogPageNum / pagesRange;

            for (int i = 1; i <= blocks; i++)
            {
                cars.AddRange(await GetAllCarsInfoAsync(firstPageInBlock, lastPageInBlock));

                firstPageInBlock += pagesRange;
                lastPageInBlock += pagesRange;
            }


            cars.AddRange(await GetAllCarsInfoAsync(firstPageInBlock, lastCatalogPageNum));

            return cars;
        }

        /// <summary>
        /// Метод, внутри которого запускаются части алгоритма для сбора данных по машинам.
        /// </summary>
        /// <param name="firstPage" name="lastPage">
        /// Диапазон страниц поиска
        /// </param>
        /// <returns>
        /// Возвращает перечисление машин.
        /// </returns>
        private async Task<IEnumerable<Car>> GetAllCarsInfoAsync(int firstPage, int lastPage)
        {
            //int lastCatalogPageNum = await GetLastCatalogPageAsync();

            List<HtmlDocument> pages = await GetSearchPagesAsync(firstPage, lastPage);

            List<HtmlNode> catalogItems = await GetAllCatalogItemsAsync(pages);

            List<HtmlNode> modificationItems = await GetAllModificationItemsAsync(catalogItems);

            List<Car> result = await GetAllCarsAsync(modificationItems);

            return result;
        }

        /// <summary>
        /// Метод для сбора всех страниц поиска с сайта
        /// </summary>
        /// <param name="lastPage">
        /// Последняя страница поиска с сайта
        /// </param>
        /// <returns>
        /// Возвращает коллекцию HTML документов страниц поиска
        /// </returns>
        private static async Task<List<HtmlDocument>> GetSearchPagesAsync(int firstPage, int lastPage)
        {
            List<Task<HtmlDocument>> pagesTasks = new List<Task<HtmlDocument>>(lastPage);
            List<HtmlDocument> pages = new List<HtmlDocument>(lastPage);

            for (int i = firstPage; i <= lastPage; i++)
            {
                string url = urlCatalog + i.ToString();

                pagesTasks.Add(Task.Run(() => GetHtmlDocumentAsync(url)));
            }

            pages.AddRange(await Task.WhenAll(pagesTasks));
            return pages;
        }

        /// <summary>
        /// Метод для сбора нод с ссылкой на страницу модели машины со страниц поиска сайта
        /// </summary>
        /// <param name="pages">
        /// Колекция страниц поиска (HTML документов)
        /// </param>
        /// <returns>
        /// Коллекцию нод с ссылкой на страницу модели машины
        /// </returns>
        private static async Task<List<HtmlNode>> GetAllCatalogItemsAsync(List<HtmlDocument> pages)
        {
            List<Task<List<HtmlNode>>> catalogItemsTasks = new List<Task<List<HtmlNode>>>();

            List<HtmlNode> catalogItems = new List<HtmlNode>();

            foreach (HtmlDocument page in pages)
            {
                catalogItemsTasks.Add(Task.Run(() => GetItemsFromCatalog(page)));
            }

            List<HtmlNode>[] catalogItemsArr = await Task.WhenAll(catalogItemsTasks);

            foreach (List<HtmlNode> item in catalogItemsArr)
            {
                catalogItems.AddRange(item);
            }

            return catalogItems;
        }

        /// <summary>
        /// Сбор нод с ссылками на страницы конкретных модификаций модели машины
        /// </summary>
        /// <param name="catalogItems">
        /// Коллекция нод с ссылками на страницы модели машины
        /// </param>
        /// <returns>
        /// Коллекцию нод с ссылками на страницы конкретных модификаций модели машины
        /// </returns>
        private async Task<List<HtmlNode>> GetAllModificationItemsAsync(List<HtmlNode> catalogItems)
        {
            List<Task<List<HtmlNode>>> modificationItemsTasks = new List<Task<List<HtmlNode>>>();

            List<HtmlNode> modificationItems = new List<HtmlNode>();

            for (int i = 0; i < catalogItems.Count; i++)
            {
                int index = i;
                modificationItemsTasks.Add(Task.Run(() => GetModificationItemsAsync(catalogItems[index])));

                if (index % 50 == 0)
                {
                    Task.WaitAll(modificationItemsTasks.ToArray());
                }
            }

            IEnumerable<HtmlNode>[] temp = await Task.WhenAll(modificationItemsTasks);

            foreach (List<HtmlNode> item in temp)
            {
                modificationItems.AddRange(item);
            }

            return modificationItems;
        }

        /// <summary>
        /// Сбор информации по каждой модификации каждой модели каждой машины
        /// </summary>
        /// <param name="modificationItems">
        /// Коллекция нод с ссылками на страницы модификаций каждой модели машины
        /// </param>
        /// <returns>
        /// Коллекцию машин (объекты инкапсулирующие информацию по машине)
        /// </returns>
        private static async Task<List<Car>> GetAllCarsAsync(List<HtmlNode> modificationItems)
        {
            List<Car> result = new List<Car>();

            List<Task<Car>> carTasks = new List<Task<Car>>(modificationItems.Count);

            for (int i = 0; i < modificationItems.Count; i++)
            {
                int index = i;
                carTasks.Add(Task.Run(() => GetModelInfoAsync(modificationItems[index])));

                if (index % 50 == 0)
                {
                    Task.WaitAll(carTasks.ToArray());
                }
            }

            result.AddRange(await Task.WhenAll(carTasks));

            return result;
        }

        /// <summary>
        /// Сбор нод с ссылками на страницы модификаций модели машины (вспомогательный метод к GetAllModificationItems)
        /// </summary>
        /// <param name="catalogItem">
        /// Нода с сылкой на страницу модели
        /// </param>
        /// <returns>
        /// Коллекцию нод с сылками на страницы модификаций модели машины
        /// </returns>
        private static async Task<List<HtmlNode>> GetModificationItemsAsync(HtmlNode catalogItem)
        {
            List<HtmlNode> modificationItems = new List<HtmlNode>();

            string link = GetCatalogItemLink(catalogItem);

            if (!string.IsNullOrWhiteSpace(link))
            {
                string url = Path.Combine(urlMain, link);

                HtmlDocument htmlDocument = await GetHtmlDocumentAsync(url);

                modificationItems = GetModificationListItems(htmlDocument);
            }

            return modificationItems;
        }

        /// <summary>
        /// Сбор информации по модификации модели машины
        /// </summary>
        /// <param name="modificationItem">
        /// Нода с ссылкой на страницу модификации машины
        /// </param>
        /// <returns>
        /// Информацию по машине
        /// </returns>
        private static async Task<Car> GetModelInfoAsync(HtmlNode modificationItem)
        {
            string link = GetModificationPageLink(modificationItem);

            HtmlDocument htmlDocument = null;

            if (!string.IsNullOrWhiteSpace(link))
            {
                string url = Path.Combine(urlMain, link);

                htmlDocument = await GetHtmlDocumentAsync(url);
            }

            return GetCarInfo(htmlDocument);
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
        private static async Task<HtmlDocument> GetHtmlDocumentAsync(string url, int atempt = 1)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument temp = null;
            try
            {
                temp = await web.LoadFromWebAsync(url);
            }
            catch (Exception)
            {
                atempt++;
                if (atempt == 10)
                {
                    throw new HtmlWebException("Impossible to connect");
                }

                await Task.Run(() => GetHtmlDocumentAsync(url, atempt));
            }

            return temp;
        }

        /// <summary>
        /// Метод для получения номера последней страницы поиска
        /// </summary>
        /// <returns>
        /// Номер последней страницы поиска
        /// </returns>
        private static async Task<int> GetLastCatalogPageAsync()
        {
            string url = Path.Combine(urlCatalog, firstCatalogPage);

            HtmlDocument htmlDocument = await GetHtmlDocumentAsync(url);

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
        /// Сбор нод с ссылками на страницы моделей машин со страницы поиска (вспомогательный к GetAllCatalogItems)
        /// </summary>
        /// <param name="doc">
        /// HTML документ со страницей поиска
        /// </param>
        /// <returns>
        /// Коллекцию нод с ссылками на страницы моделей
        /// </returns>
        private static List<HtmlNode> GetItemsFromCatalog(HtmlDocument doc)
        {
            return doc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("p-search__item js-module link-hdr"))
                .ToList();
        }

        /// <summary>
        /// Метод для получения пути к странице с модификациями модели (вспомогательный к GetModificationItems)
        /// </summary>
        /// <param name="node">
        /// Нода с ссылкой на страницу с модификациями модели
        /// </param>
        /// <returns>
        /// Путь к странице с модификациями модели
        /// </returns>
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
        /// Сбор нод с ссылками на страницы модификаций модели машины (вспомогательный к GetModificationItems)
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static List<HtmlNode> GetModificationListItems(HtmlDocument doc)
        {
            return doc.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("cell padding_10 padding_bottom_5"))
                .ToList();
        }

        /// <summary>
        /// Метод для получения ссылки на страницу модификации модели машины (вспомогательный к GetModelInfo)
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
        /// Метод для получения информации по модификации модли машины (вспомогательный к GetModelInfo)
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
