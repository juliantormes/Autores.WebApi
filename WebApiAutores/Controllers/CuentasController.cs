﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using WebApiAutores.DTOs;
using WebApiAutores.Servicios;

namespace WebApiAutores.Controllers
{
    [ApiController]
    [Route("api/cuentas")]
    public class CuentasController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly HashService _hashService;
        private readonly IDataProtector _dataProtector;

        public CuentasController(UserManager<IdentityUser> userManager,
            IConfiguration configuration,
            SignInManager<IdentityUser> signInManager,
            IDataProtectionProvider dataProtectionProvider,
            HashService hashService) 
        {

            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _hashService = hashService;
            _dataProtector = dataProtectionProvider.CreateProtector("valor_unico_y_quizas_secreto");
        }
        [HttpGet("hash/{textoplano}")]
        public ActionResult RealizarHash(string textoPlano)
        {
            var resultado1 = _hashService.Hash(textoPlano);
            var resultado2 = _hashService.Hash(textoPlano);
            return Ok(new {
                textoPlano= textoPlano,
                Hash1= resultado1,
                Hash2= resultado2
            });
        }
        [HttpGet("encriptar")]
        public ActionResult Encriptar()
        {
            var textoPlano = "Julian Tormes";
            var textoCifrado = _dataProtector.Protect(textoPlano);
            var textoDesencriptado = _dataProtector.Unprotect(textoCifrado);
            return Ok(new
            {
                textoPlano = textoPlano,
                textoCifrado = textoCifrado,
                textoDesencriptado = textoDesencriptado
            });
        }
        [HttpGet("encriptarPorTiempo")]
        public ActionResult EncriptarPorTiempo()
        {
            var protectorLimitadoPorTiempo = _dataProtector.ToTimeLimitedDataProtector();
            var textoPlano = "Julian Tormes";
            var textoCifrado = protectorLimitadoPorTiempo.Protect(textoPlano, lifetime:TimeSpan.FromSeconds(5));
            Thread.Sleep(6000);
            var textoDesencriptado = _dataProtector.Unprotect(textoCifrado);
            return Ok(new
            {
                textoPlano= textoPlano,
                textoCifrado = textoCifrado,
                textoDesencriptado=textoDesencriptado
            });
        }
        [HttpPost("Registar")] //api/cuentas/registrar
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuario creedencialesUsuario)
        {
            var usuario = new IdentityUser { UserName = creedencialesUsuario.Email,
                Email = creedencialesUsuario.Email };
            var resultado = await _userManager.CreateAsync(usuario, creedencialesUsuario.Password);
            if (resultado.Succeeded)
            {
                return await ConstruirToken(creedencialesUsuario);
            }
            else
            {
                return BadRequest(resultado.Errors);
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuario credencialesUsuario)
        {
            var resultado = await _signInManager.PasswordSignInAsync(credencialesUsuario.Email,
                credencialesUsuario.Password, isPersistent: false, lockoutOnFailure: false);
            if (resultado.Succeeded)
            {
                return await ConstruirToken(credencialesUsuario);
            }
            else
            {
                return BadRequest("Login incorrecto");
            }
        }
        [HttpGet("RenovarToken")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async  Task <ActionResult<RespuestaAutenticacionDTO>> Renovar()
        {
            var emailClaim = HttpContext.User.Claims.Where(claim => claim.Type == "email").FirstOrDefault();
            var email = emailClaim.Value;
            var credencialesUsuario = new CredencialesUsuario()
            {
                Email= email
            };
            return await ConstruirToken(credencialesUsuario);
            
        }
        private async Task <RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuario creedencialesUsuario)
        {
            var claims = new List<Claim>()
            {
               new Claim ("email", creedencialesUsuario.Email),
               new Claim ("lo que yo quiera","Cualquier otro valor")
            };
            var usuario = await _userManager.FindByEmailAsync(creedencialesUsuario.Email);
            var claimsDB= await _userManager.GetClaimsAsync(usuario);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["llavejwt"]));
            var creds = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);
            var expiracion = DateTime.UtcNow.AddYears(1);
            var securityToken = new JwtSecurityToken(issuer: null, audience: null, claims: claims,
                expires: expiracion, signingCredentials:creds);
            return new RespuestaAutenticacionDTO()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(securityToken),
                Expiracion = expiracion,
            };
        }
        [HttpPost("HacerAdmin")]
        public async Task<ActionResult> HacerAdmin(EditarAdminDTO editarAdminDTO)
        { 
            var usuario = await _userManager.FindByEmailAsync(editarAdminDTO.Email);
            await _userManager.AddClaimAsync(usuario, new Claim("esAdmin", "1"));
            return NoContent();
        }
        [HttpPost("RemoverAdmin")]
        public async Task<ActionResult> RemoverAdmin(EditarAdminDTO editarAdminDTO)
        {
            var usuario = await _userManager.FindByEmailAsync(editarAdminDTO.Email);
            await _userManager.RemoveClaimAsync(usuario, new Claim("esAdmin", "1"));
            return NoContent();
        }
    }
}
