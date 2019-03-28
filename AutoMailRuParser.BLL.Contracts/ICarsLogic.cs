using System;
using System.Collections.Generic;
using AutoMailRuParser.Entities;

namespace AutoMailRuParser.BLL.Contracts
{
    public interface ICarsLogic
    {
        IEnumerable<Car> GetAllCarsInfo();
    }
}
