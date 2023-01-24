﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebApiAutores.Controllers.V1;
using WebAPIAutores.Test.Mocks;

namespace WebAPIAutores.Test.PruebasUnitarias
{
    [TestClass]
    public class RootControllerTest
    {
        [TestMethod]
        public async Task SiUsuarioEsAdmin_Obtenemos4links()
        {

            //Preparacion
            var authorizationService = new AuthorizationServicesMock();
            authorizationService.Resultado = AuthorizationResult.Success();
            var rootController = new RootController(authorizationService);
            rootController.Url = new URLHelperMock();

            //Ejecucion
            var resultado = await rootController.Get();
            //Verificacion
            Assert.AreEqual(4, resultado.Value.Count());
        }
        [TestMethod]
        public async Task SiUsuarioNoEsAdmin_Obtenemos2links()
        {

            //Preparacion
            var authorizationService = new AuthorizationServicesMock();
            authorizationService.Resultado = AuthorizationResult.Failed();
            var rootController = new RootController(authorizationService);
            rootController.Url = new URLHelperMock();

            //Ejecucion
            var resultado = await rootController.Get();
            //Verificacion
            Assert.AreEqual(2, resultado.Value.Count());
        }
        [TestMethod]
        public async Task SiUsuarioNoEsAdmin_Obtenemos2linksMOQ()
        {

            //Preparacion
            var mockAuthorizationService = new Mock<IAuthorizationService>();
            mockAuthorizationService.Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()
                )).Returns(Task.FromResult(AuthorizationResult.Failed()));
            mockAuthorizationService.Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<string>()
                )).Returns(Task.FromResult(AuthorizationResult.Failed()));

            var mockURLHelper = new Mock<IUrlHelper>();
            mockURLHelper.Setup(x => x.Link(It.IsAny<string>(),
                It.IsAny<object>()))
                .Returns(string.Empty);

            var rootController = new RootController(mockAuthorizationService.Object);
            rootController.Url = mockURLHelper.Object;

            //Ejecucion
            var resultado = await rootController.Get();
            //Verificacion
            Assert.AreEqual(2, resultado.Value.Count());
        }
    }
}
