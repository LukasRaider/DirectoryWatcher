using Microsoft.AspNetCore.Mvc;
using DirectoryWatcher.Models;
using DirectoryWatcher.Services;
using System.Threading.Tasks;

namespace DirectoryWatcher.Controllers
{
	public class HomeController : Controller
	{
		private readonly IDirectoryAnalyzerService _analyzerService;

		
		public HomeController(IDirectoryAnalyzerService analyzerService)
		{
			_analyzerService = analyzerService;
		}

		// GET: Zobrazení úvodní stránky s formuláøem
		[HttpGet]
		public IActionResult Index()
		{
			return View(new AnalysisResult());
		}

		// POST: Zpracování formuláøe po kliknutí na tlaèítko "Analyzovat"
		[HttpPost]
		public async Task<IActionResult> Index(string directoryPath)
		{
			// Základní validace vstupu pøímo na Controlleru
			if (string.IsNullOrWhiteSpace(directoryPath))
			{
				var emptyResult = new AnalysisResult
				{
					ErrorMessage = "Cesta k adresáøi nesmí být prázdná."
				};
				return View(emptyResult);
			}

			// Volání byznysové logiky
			var result = await _analyzerService.AnalyzeDirectoryAsync(directoryPath);

			// Vracení stejné View, ale tentokrát mu pøedáme naplnìný model s výsledky analýzy
			return View(result);
		}
	}
}