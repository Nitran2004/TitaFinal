using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using static ProyectoIdentity.Datos.ApplicationDbContext;

namespace ProyectoIdentity.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionPointsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CollectionPointsController(ApplicationDbContext db) => _db = db;

        // GET /api/collectionpoints
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollectionPoint>>> GetAll()
            => await _db.CollectionPoints.ToListAsync();

        // (Opcional) GET /api/collectionpoints/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CollectionPoint>> Get(int id)
        {
            var pt = await _db.CollectionPoints.FindAsync(id);
            if (pt == null) return NotFound();
            return pt;
        }
    }
}
