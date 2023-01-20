﻿using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiAutores.DTOs;
using WebApiAutores.Entidades;
using WebApiAutores.Filtros;

namespace WebApiAutores.Controllers
{
    [ApiController]
    [Route("api/autores")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EsAdmin")]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        //private readonly IValidator<Autor> _validator;

        public AutoresController(ApplicationDbContext context,IMapper mapper,IConfiguration configuration) 

        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }
        [HttpGet]//api/autores (hereda ruta)
        //(Multiples rutas)
        [AllowAnonymous]
        public async Task<ActionResult<List<AutorDTO>>> Get()
        {
            var autores = await _context.Autores.ToListAsync();
            return _mapper.Map<List<AutorDTO>>(autores);
        }
        [HttpGet("{id:int}",Name = "obtenerAutor")]
        public async Task<ActionResult<AutorDTOConLibros>> Get(int id)
        {
            var autor = await _context.Autores
                .Include(autorDB=>autorDB.AutorLibro )
                .ThenInclude(autorLibroDB=>autorLibroDB.Libro)
                .FirstOrDefaultAsync(autorBD => autorBD.Id == id);

            if (autor == null)
            {
                return NotFound();
            }
            return _mapper.Map<AutorDTOConLibros>(autor);

        }
        [HttpGet("{nombre}")]
        public async Task<ActionResult<List<AutorDTO>>> Get([FromRoute] string nombre)
        {
            var autores = await _context.Autores.Where(autorBD => autorBD.Nombre.Contains(nombre)).ToListAsync();

            return _mapper.Map<List<AutorDTO>>(autores);

        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] AutorCreacionDTO autorCreacionDTO)
        {
            /*var result = await ValidateAsync(autor);
            if (!result.IsValid)
            {
                _logger.LogWarning("{@errors}", result.Errors);
                return BadRequest(result.Errors);
            }*/
            var ExisteAutorConElMismoNombre = await _context.Autores.AnyAsync(x=>x.Nombre==autorCreacionDTO.Nombre);
            if (ExisteAutorConElMismoNombre)
            {
                return BadRequest($"Ya existe un autor con el nombre{autorCreacionDTO.Nombre}");
            }

            var autor = _mapper.Map<Autor>(autorCreacionDTO);

            _context.Add(autor);
            await _context.SaveChangesAsync();

            var autorDTO = _mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("obtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}")]// api/autores/"IdAutor"
        public async Task<ActionResult> Put(AutorCreacionDTO autorCreacionDTO, int id)
        {
            var existe = await _context.Autores.AnyAsync(autorBD => autorBD.Id == id);
            if (!existe)
            {
                return NotFound();
            }
            var autor = _mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;
            _context.Update(autor);
            await _context.SaveChangesAsync();
            return NoContent();

        }
        [HttpDelete("{id:int}")] // api/autores/IdAutor
        public async Task<ActionResult> Delete(int id)
        {
            var existe = await _context.Autores.AnyAsync(x => x.Id == id);
            if (!existe)
            {
                return NotFound();
            }

            _context.Remove(new Autor() { Id = id });
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
