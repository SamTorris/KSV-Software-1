﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using VotingApp.DAL.Abstract;
using VotingApp.Models;
using VotingApp.ViewModel;

namespace VotingApp.Controllers
{
    public class CreateController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICreatedVoteRepository _createdVoteRepository;
        private readonly IVoteTypeRepository _voteTypeRepository;
        private readonly VoteCreationService _voteCreationService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IVotingUserRepositiory _votingUserRepository;

        public CreateController(ILogger<HomeController> logger, 
            ICreatedVoteRepository createdVoteRepo, 
            IVoteTypeRepository voteTypeRepository, 
            VoteCreationService voteCreationService, 
            UserManager<IdentityUser> userManager,
            IVotingUserRepositiory votingUserRepositiory)
        {
            _logger = logger;
            _createdVoteRepository = createdVoteRepo;
           _voteTypeRepository = voteTypeRepository;
            _voteCreationService = voteCreationService;
            _userManager = userManager;
            _votingUserRepository = votingUserRepositiory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var selectListVoteType = new SelectList(
                _voteTypeRepository.VoteTypes().Select(a => new { Text = $"{a.VotingType}", Value = a.Id }),
                "Value", "Text");
            ViewData["VoteTypeId"] = selectListVoteType;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index([Bind("VoteTypeId,VoteTitle,VoteDiscription,Anonymous")]CreatedVote createdVote)
        {
            ModelState.Remove("VoteType");
            ModelState.Remove("VoteAccessCode");
            
            if (User.Identity.IsAuthenticated != false)
            {
                createdVote.User = _votingUserRepository.GetUserByAspId(_userManager.GetUserId(User));
            }
            if (ModelState.IsValid)
            {
                if (createdVote.VoteTypeId == 1)
                {   
                    
                    createdVote.VoteOptions = _voteTypeRepository.CreateVoteOptions();
                }
                try
                {
                    createdVote.VoteAccessCode = _voteCreationService.generateCode();
                    _createdVoteRepository.AddOrUpdate(createdVote);
                }
                catch (DbUpdateConcurrencyException e)
                {
                    ViewBag.Message = "A concurrency error occurred while trying to create the expedition. Please try again";
                    return View(createdVote);
                }
                catch (DbUpdateException e)
                {
                    ViewBag.Message = "An unknown database error occurred while trying to create the item. Please try again";
                    return View(createdVote);
                }

                if (createdVote.VoteTypeId == 1)
                {
                    return RedirectToAction("Confirmation", createdVote);
                }
                else
                {
                    createdVote = _createdVoteRepository.GetById(createdVote.Id);
                    return View("MultipleChoice", createdVote);
                }

            }
            else
            {
                ViewBag.Message = "An unknown database error occurred while trying to create the item. Please try again.";
                return View(createdVote);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult edit([Bind("Id,VoteTypeId,VoteTitle,VoteDiscription,Anonymous")] CreatedVote createdVote)
        {
            ModelState.Remove("VoteType");
            ModelState.Remove("VoteAccessCode");
            var currentVote = _createdVoteRepository.GetById(createdVote.Id); //this is for checking what the vote is in the db
            if (User.Identity.IsAuthenticated != false)
            {
                createdVote.User = _votingUserRepository.GetUserByAspId(_userManager.GetUserId(User));
            }
            if (ModelState.IsValid)
            {
                if (createdVote.VoteTypeId == 1)
                {
                    //remove previous options before adding in the new ones
                    createdVote.VoteOptions = _voteTypeRepository.CreateVoteOptions();
                }
                if (currentVote.VoteTypeId == 1 && createdVote.VoteTypeId != 1) //going from yes/no to any other type of vote
                {
                    //remove all options
                }
                try
                {

                    createdVote.VoteAccessCode = _voteCreationService.generateCode();
                    createdVote = _createdVoteRepository.AddOrUpdate(createdVote);
                }
                catch (DbUpdateConcurrencyException e)
                {
                    ViewBag.Message = "A concurrency error occurred while trying to create the expedition. Please try again";
                    return View(createdVote);
                }
                catch (DbUpdateException e)
                {
                    ViewBag.Message = "An unknown database error occurred while trying to create the item. Please try again";
                    return View(createdVote);
                }

                if (createdVote.VoteTypeId == 1)
                {
                    return RedirectToAction("Confirmation", createdVote);
                }
                else
                {
                    createdVote = _createdVoteRepository.GetById(createdVote.Id);
                    return View("MultipleChoice", createdVote);
                }

            }
            else
            {
                ViewBag.Message = "An unknown database error occurred while trying to create the item. Please try again.";
                return View(createdVote);
            }
        }

        [HttpGet]
        public IActionResult MultipleChoice(CreatedVote createdVote)
        {
            var vote = _createdVoteRepository.GetById(createdVote.Id);
            return View(vote);
        }

        [HttpGet]
        public IActionResult AddMultipleChoiceOption(int id, string option)
        {
            //var vm = new MultipleChoiceVM();
            //vm.createdVote = _createdVoteRepository.GetById(id);
            //vm.votingOptions.VoteOptionString = option;
            //_createdVoteRepository.AddOrUpdate(vm.createdVote); 
            //return View(vm); 

            var vote = _createdVoteRepository.GetById(id);
            VoteOption voteOption = new VoteOption();
            voteOption.VoteOptionString = option;
            vote.VoteOptions.Add(voteOption);
            _createdVoteRepository.AddOrUpdate(vote);
            return RedirectToAction("MultipleChoice", vote);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOption()
        {
            return RedirectToAction("MultipleChoice"); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MultipleChoiceToConfirmation(int id)
        {
            var createdVote = _createdVoteRepository.GetById(id);
            return RedirectToAction("Confirmation", createdVote);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveOption(int optionId)
        {
            //REMOVE FROM DATABASE BY ID
            return RedirectToAction("MultipleChoice");
        }

        [HttpGet]
        public IActionResult Confirmation(CreatedVote createdVote)
        {
            createdVote = _createdVoteRepository.GetById(createdVote.Id);
            var vm = new ConfirmationVM();
            vm.VoteTitle = _createdVoteRepository.GetVoteTitle(createdVote.Id);
            vm.VoteDescription = _createdVoteRepository.GetVoteDescription(createdVote.Id);
            vm.VoteType = _voteTypeRepository.GetVoteType(createdVote.VoteTypeId);
            vm.ChosenVoteDescriptionHeader = _voteTypeRepository.GetChosenVoteHeader(vm.VoteType);
            vm.VotingOptions = createdVote.VoteOptions.ToList();
            vm.ID = createdVote.Id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirmation()
        {
            return RedirectToAction("Index", "Home");
        }

        //[HttpGet]
        public IActionResult BackToIndexPage(int ID)
        {
            var result = _createdVoteRepository.GetById(ID);
            var selectListVoteType = new SelectList(
                _voteTypeRepository.VoteTypes().Select(a => new { Text = $"{a.VotingType}", Value = a.Id }),
                "Value", "Text");
            ViewData["VoteTypeId"] = selectListVoteType;
            return View("Index",result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
