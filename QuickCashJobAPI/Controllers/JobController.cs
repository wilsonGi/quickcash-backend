using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Enums;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Services;
using Microsoft.AspNetCore.Identity;

namespace QuickCashJobAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;
        public JobController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IUserService userService)
        {
            _db = db;
            _userManager = userManager;
            _userService = userService;
        }

        private ApplicationUser GetCurrentUser()
        {
            var userClaims = HttpContext.User.Identity as ClaimsIdentity;
            if (userClaims == null)
            {
                return null;
            }

            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return _db.Users.OfType<ApplicationUser>().FirstOrDefault(u => u.Id == userId);
        }



        [HttpGet("GetAllSkills")]
        public async Task<IActionResult> GetAllSkills()
        {
            var skills = await _db.Skills.ToListAsync();
            return Ok(skills);
        }


        [Authorize]
        [HttpPost("AddSkills")]
        public async Task<IActionResult> AddSkills([FromBody] UpdateUserSkillsDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userManager.Users
                .Include(u => u.UserSkills)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) 
                return NotFound(new { message = "User not found" });

            var existingSkillIds = user.UserSkills.Select(us => us.SkillId).ToHashSet();

            foreach (var skillId in dto.SkillIds)
            {
                if (!existingSkillIds.Contains(skillId))
                {
                    user.UserSkills.Add(new UserSkill { UserId = userId, SkillId = skillId });
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Skills added successfully" });
        }


        [Authorize]
        [HttpGet("GetUserSkills")]
        public async Task<IActionResult> GetUserSkills()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userManager.Users
                .Include(u => u.UserSkills)
                .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            var skills = user.UserSkills.Select(us => new
            {
                us.Skill.Id,
                us.Skill.Name
            });

            return Ok(skills);
        }


        [Authorize]
        [HttpDelete("RemoveSkill/{skillId}")]
        public async Task<IActionResult> RemoveSkill(int skillId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userSkill = await _db.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

            if (userSkill == null)
            {
                return NotFound(new { message = "Skill not associated with user" });
            }

            _db.UserSkills.Remove(userSkill);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Skill removed successfully" });
        }



        [Authorize(Roles = SD.Role_Admin)]
        [HttpGet("categories")]
        public ActionResult<IEnumerable<CategoryDTO>> GetCategories()
        {
            var categories = _db.Categories.Select(c => new CategoryDTO
            {
                Id = c.Id,
                CategoryName = c.CategoryName,
                NumberOfInstances = c.NumberOfInstances
            }).ToList();

            return Ok(categories);
        }



        [Authorize]
        [HttpGet("myjobs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<JobDTO>> GetMyJobs()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            var jobs = _db.Jobs
                .Where(job => job.UserId == currentUser.Id) // Include only jobs created by the current user
                .Select(job => new JobDTO
                {
                    Id = job.Id,
                    CategoryId = job.CategoryId,
                    Description = job.Description,
                    Location = job.Location,
                    Status = job.Status,
                    DatePosted = job.DatePosted,
                    AudioDescription = job.AudioDescription,
                    Payout = job.Payout,
                    Negotiable = job.Negotiable,
                    UserName = job.UserName,
                    NumberOfTasksCompleted = job.NumberOfTasksCompleted,
                    NumberOfTasksEmployed = job.NumberOfTasksEmployed,
                    UserLastTaskEmployedDate = job.UserLastTaskEmployedDate,
                    UserRating = job.UserRating,
                    UserPhoneNumber = job.UserPhoneNumber,
                    ShowContact = job.ShowContact,

                }).ToList();

            return Ok(jobs);
        }


        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<JobDTO>> GetJobs()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var user = GetCurrentUser();
            if (!user.IsApproved || user.IsBlocked)
            {
                return Forbid("Your account is not approved or is blocked.");
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }

            var jobs = _db.Jobs
                .Where(job => job.UserId != currentUser.Id)
                .Select(job => new JobDTO
                {
                    Id = job.Id,
                    CategoryId = job.CategoryId,
                    CategoryName = job.Category.CategoryName,
                    Description = job.Description,
                    Location = job.Location,
                    Status = job.Status,
                    DatePosted = job.DatePosted,
                    AudioDescription = job.AudioDescription,
                    Payout = job.Payout,
                    Negotiable = job.Negotiable,
                    UserName = job.UserName,
                    NumberOfTasksCompleted = job.NumberOfTasksCompleted,
                    NumberOfTasksEmployed = job.NumberOfTasksEmployed,
                    UserLastTaskDoneDate = job.UserLastTaskDoneDate,
                    UserLastTaskEmployedDate = job.UserLastTaskEmployedDate,
                    UserRating = job.UserRating,
                    UserPhoneNumber = job.UserPhoneNumber,
                    ShowContact = job.ShowContact,
                }).ToList();

            return Ok(jobs);
        }


        [Authorize]
        [HttpGet("{id:int}", Name = "GetJob")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<JobDTO> GetJob(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }

            var user = GetCurrentUser();
            if (!user.IsApproved || user.IsBlocked)
            {
                return Forbid("Your account is not approved or is blocked.");
            }

            var job = _db.Jobs.Select(job => new JobDTO
            {
                Id = job.Id,
                CategoryId = job.CategoryId,
                CategoryName = job.Category.CategoryName,
                Description = job.Description,
                Location = job.Location,
                Status = job.Status,
                DatePosted = job.DatePosted,
                AudioDescription = job.AudioDescription,
                Payout = job.Payout,
                Negotiable = job.Negotiable,
                UserName = job.UserName,
                NumberOfTasksCompleted = job.NumberOfTasksCompleted,
                NumberOfTasksEmployed = job.NumberOfTasksEmployed,
                UserLastTaskDoneDate = job.UserLastTaskDoneDate,
                UserLastTaskEmployedDate = job.UserLastTaskEmployedDate,
                UserRating = job.UserRating,
                UserPhoneNumber = job.UserPhoneNumber,
                ShowContact = job.ShowContact,
            }).FirstOrDefault(u => u.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Get the UserId separately
            var jobOwnerId = _db.Jobs.Where(j => j.Id == id).Select(j => j.UserId).FirstOrDefault();

            var currentUser = GetCurrentUser();
            if (currentUser == null || jobOwnerId != currentUser.Id)
            {
                return Unauthorized();
            }

            return Ok(job);
        }


        [Authorize]
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<JobDTO>> GetAllJobs()
        {
            var user = GetCurrentUser();
            if (user == null)
                return Unauthorized();

            if (!user.IsApproved || user.IsBlocked)
                return Forbid("Your account is not approved or is blocked.");

            var jobs = _db.Jobs
                .Select(job => new JobDTO
                {
                    Id = job.Id,
                    CategoryId = job.CategoryId,
                    CategoryName = job.Category.CategoryName,
                    Description = job.Description,
                    Location = job.Location,
                    Status = job.Status,
                    DatePosted = job.DatePosted,
                    AudioDescription = job.AudioDescription,
                    Payout = job.Payout,
                    Negotiable = job.Negotiable,
                    UserName = job.UserName,
                    NumberOfTasksCompleted = job.NumberOfTasksCompleted,
                    NumberOfTasksEmployed = job.NumberOfTasksEmployed,
                    UserLastTaskDoneDate = job.UserLastTaskDoneDate,
                    UserLastTaskEmployedDate = job.UserLastTaskEmployedDate,
                    UserRating = job.UserRating,
                    UserPhoneNumber = job.UserPhoneNumber,
                    ShowContact = job.ShowContact,
                }).ToList();

            return Ok(jobs);
        }


        [HttpPost("toggle-show-contact/{jobId}")]
        public async Task<IActionResult> ToggleShowContact(int jobId)
        {
            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null)
            {
                return NotFound(); // Return 404 if the job doesn't exist
            }

            // Toggle the ShowContact value
            job.ShowContact = !job.ShowContact;

            // Save the changes to the database
            await _db.SaveChangesAsync();

            // Return the updated job to the client
            return Ok(job); // Return the updated job details (including ShowContact status)
        }



        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(JobDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<JobDTO> CreateJob([FromBody] JobCreateDTO jobCreateDTO)
        {
            if (jobCreateDTO == null)
            {
                return BadRequest();
            }


            var category = _db.Categories.FirstOrDefault(c => c.Id == jobCreateDTO.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = "Invalid Category ID" });
            }


            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }

            if (!user.IsApproved || user.IsBlocked)
            {
                return Forbid("Your account is not approved or is blocked.");
            }

            var job = new Job
            {
                CategoryId = jobCreateDTO.CategoryId,
                Description = jobCreateDTO.Description,
                Location = jobCreateDTO.Location,
                Status = JobStatus.Active, // Automatically set to Active
                DatePosted = DateTime.UtcNow,
                AudioDescription = jobCreateDTO.AudioDescription,
                Payout = jobCreateDTO.Payout,
                Negotiable = jobCreateDTO.Negotiable,
                UserLocation = user.Location,
                UserName = user.Name,
                UserLastTaskDoneDate = user.LastTaskDoneDate,
                UserLastTaskEmployedDate = user.LastTaskEmployedDate,
                UserRating = user.UserRating,
                UserPhoneNumber = user.PhoneNumber,
                UserId = user.Id // Set the UserId
            };

            category.NumberOfInstances++;
            _db.Jobs.Add(job);
            _db.SaveChanges();

            var jobDto = new JobDTO
            {
                Id = job.Id,
                CategoryId = job.CategoryId,
                Description = job.Description,
                Location = job.Location,
                Status = job.Status,
                DatePosted = job.DatePosted,
                AudioDescription = job.AudioDescription,
                Payout = job.Payout,
                Negotiable = job.Negotiable,
                UserName = job.UserName,
                NumberOfTasksCompleted = job.NumberOfTasksCompleted,
                NumberOfTasksEmployed = job.NumberOfTasksEmployed,
                UserLastTaskDoneDate = job.UserLastTaskDoneDate,
                UserLastTaskEmployedDate = job.UserLastTaskEmployedDate,
                UserRating = job.UserRating,
                UserPhoneNumber = job.UserPhoneNumber
            };

            return CreatedAtRoute("GetJob", new { id = job.Id }, jobDto);
        }


        [Authorize]
        [HttpPost("commit/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CommitToJob(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job not found." });
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }


            if (!user.IsApproved || user.IsBlocked)
            {
                return StatusCode(403, new { message = "Your account is not approved or is blocked." });
            }

            if (job.UserId == user.Id)
            {
                return BadRequest(new { message = "You cannot commit to your own job." });
            }

            // Check if the user has already committed to this job
            bool alreadyCommitted = await _db.JobCommitments
                .AnyAsync(jc => jc.JobId == id && jc.ContractorId == user.Id);
            if (alreadyCommitted)
            {
                return BadRequest(new { message = "You have already committed to this job." });
            }

            // Add new commitment record
            var commitment = new JobCommitment
            {
                JobId = id,
                ContractorId = user.Id,
                ContractorName = user.Name,
                CommittedAt = DateTime.UtcNow
            };

            _db.JobCommitments.Add(commitment);

            // ✅ Corrected job status update logic
            bool hasExistingCommitments = await _db.JobCommitments.AnyAsync(jc => jc.JobId == id);
            if (!hasExistingCommitments)
            {
                job.Status = JobStatus.Committed;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Job committed successfully." });
        }



        [Authorize]
        [HttpGet("committed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<JobDTO>> GetCommittedJobs()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized("User not found.");
            }

            var committedJobs = _db.JobCommitments
                .Where(jc => jc.ContractorId == currentUser.Id) // Filter by committed jobs for current user
                .Select(jc => new JobDTO
                {
                    Id = jc.Job.Id,
                    CategoryId = jc.Job.CategoryId,
                    Description = jc.Job.Description,
                    Location = jc.Job.Location,
                    Status = jc.Job.Status,
                    DatePosted = jc.Job.DatePosted,
                    AudioDescription = jc.Job.AudioDescription,
                    Payout = jc.Job.Payout,
                    Negotiable = jc.Job.Negotiable,
                    UserName = jc.Job.UserName,
                    NumberOfTasksCompleted = jc.Job.NumberOfTasksCompleted,
                    NumberOfTasksEmployed = jc.Job.NumberOfTasksEmployed,
                    UserLastTaskDoneDate = jc.Job.UserLastTaskDoneDate,
                    UserLastTaskEmployedDate = jc.Job.UserLastTaskEmployedDate,
                    UserRating = jc.Job.UserRating,
                    UserPhoneNumber = jc.Job.UserPhoneNumber
                })
                .ToList();

            return Ok(committedJobs);
        }


        [HttpGet("committed-contractors/{jobId:int}")]
        public async Task<IActionResult> GetCommittedContractors(int jobId)
        {
            // Check if the job exists
            bool jobExists = await _db.Jobs.AnyAsync(j => j.Id == jobId);
            if (!jobExists)
            {
                return NotFound(new { message = "Job not found." });
            }

                    var committedContractors = await _db.JobCommitments
            .Where(jc => jc.JobId == jobId)
            .Include(jc => jc.Contractor) // Load Contractor details
            .Select(jc => new ContractorDTO
            {
                Id = jc.Contractor.Id,
                Name = jc.Contractor.Name,
                Location = jc.Contractor.Location,
                NumberOfTasksCompleted = jc.Contractor.NumberOfTasksCompleted,
                NumberOfTasksEmployed = jc.Contractor.NumberOfTasksEmployed,
                LastTaskDoneDate = jc.Contractor.LastTaskDoneDate,
                LastTaskEmployedDate = jc.Contractor.LastTaskEmployedDate,
                UserRating = jc.Contractor.UserRating,
                DateJoined = jc.Contractor.DateJoined,
                IsDeleted = jc.Contractor.IsDeleted,
                IsBlocked = jc.Contractor.IsBlocked,
                IsApproved = jc.Contractor.IsApproved,
                IsAdmin = jc.Contractor.IsAdmin,
                Latitude = jc.Contractor.Latitude,
                Longitude = jc.Contractor.Longitude,
                TrialEndDate = jc.Contractor.TrialEndDate,
                IsSubscriptionActive = jc.Contractor.IsSubscriptionActive,
                UserName = jc.Contractor.UserName,
                Email = jc.Contractor.Email,
                PhoneNumber = jc.Contractor.PhoneNumber,
                Skills = _db.UserSkills
                    .Where(us => us.UserId == jc.Contractor.Id)
                    .Select(us => us.Skill.Name)
                    .ToList()
            })
            .ToListAsync();


            // If no contractors have committed, return an empty list
            if (!committedContractors.Any())
            {
                return Ok(new { message = "No contractors have committed to this job yet.", data = committedContractors });
            }

            return Ok(committedContractors);
        }


        [Authorize]
        [HttpPost("approve/{jobId:int}/{contractorId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveContractorForJob(int jobId, string contractorId, [FromBody] LocationDTO location)
        {
            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null)
            {
                return NotFound(new { message = "Job not found." });
            }

            var user = GetCurrentUser();
            if (user == null || !user.IsApproved || user.IsBlocked)
            {
                return Unauthorized(new { message = "User is not authorized." });
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }

            if (job.UserId != user.Id)
            {
                return BadRequest(new { message = "You cannot approve a contractor for a job you didn't create." });
            }

            // Ensure that contractorId is a string when comparing
            var jobCommitment = await _db.JobCommitments
                .FirstOrDefaultAsync(jc => jc.JobId == jobId && jc.ContractorId == contractorId.ToString());

            if (jobCommitment == null)
            {
                return NotFound(new { message = "Contractor has not committed to this job." });
            }

            jobCommitment.IsApproved = true; // Mark the contractor as approved
            job.Status = JobStatus.Approved;
            job.ApprovalLatitude = location.Latitude;
            job.ApprovalLongitude = location.Longitude;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Contractor approved successfully for the job." });
        }


        [Authorize]
        [HttpPost("disapprove/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DisapproveTask(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job not found." });
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }

            if (!user.IsApproved || user.IsBlocked)
            {
                return Forbid("Your account is not approved or is blocked.");
            }

            if (job.UserId != user.Id)
            {
                return BadRequest(new { message = "You cannot disapprove a task you didn't create." });
            }

            job.Status = JobStatus.Active;
            job.ContractorId = null;
            job.ContractorName = null;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Task disapproved successfully and made active again." });
        }



        [Authorize]
        [HttpPost("confirm/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmTask(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job not found." });
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }

            if (!user.IsApproved || user.IsBlocked)
            {
                return Forbid("Your account is not approved or is blocked.");
            }

            if (job.UserId == user.Id)
            {
                return BadRequest(new { message = "You cannot confirm your own job." });
            }

            job.Status = JobStatus.Confirmed;
            job.ContractorId = user.Id;
            job.ContractorName = user.Name;

            
            var contractor = await _db.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == job.ContractorId);
            if (contractor == null)
            {
                return NotFound(new { message = "Contractor not found." });
            }

            
            //contractor.LastTaskDoneDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Task confirmed successfully." });
        }



        [Authorize]
        [HttpPost("task-completed/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TaskCompleted(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job not found." });
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            if (!HasValidSubscription(user))
            {
                return Forbid();
            }


            if (!user.IsApproved || user.IsBlocked)
            {
                return Forbid("Your account is not approved or is blocked.");
            }

            // Ensure the user marking the job as completed is the job creator
            if (job.UserId != user.Id)
            {
                return BadRequest(new { message = "You are not authorized to complete this task." });
            }

            // Ensure the job has been confirmed by the contractor
            if (job.Status != JobStatus.Confirmed)
            {
                return BadRequest(new { message = "The job must be confirmed by the contractor before it can be marked as completed." });
            }

            job.Status = JobStatus.Completed;

            var contractor = await _db.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == job.ContractorId);
            if (contractor == null)
            {
                return NotFound(new { message = "Contractor not found." });
            }

            // Update contractor's task count
            contractor.NumberOfTasksCompleted++;
            contractor.UserRating = CalculateUserRating(contractor.UserRating + 10);
            contractor.LastTaskDoneDate = DateTime.UtcNow;

            // Update creator's task count
            user.NumberOfTasksEmployed++;
            user.UserRating = CalculateUserRating(user.UserRating + 10);
            user.LastTaskEmployedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Task marked as completed." });
        }



        private int CalculateUserRating(double currentPoints)
        {
            int ratingPercentage;

            if (currentPoints >= 500)
            {
                ratingPercentage = 100;
            }
            else if (currentPoints >= 300)
            {
                ratingPercentage = 80;
            }
            else if (currentPoints >= 200)
            {
                ratingPercentage = 50;
            }
            else
            {
                ratingPercentage = 20;
            }

            return ratingPercentage;
        }


        [Authorize]
        [HttpPatch("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdatePartialJob(int id, [FromBody] JsonPatchDocument<JobUpdateDTO> patchDto)
        {
            if (patchDto == null || id == 0)
            {
                return BadRequest();
            }

            var job = _db.Jobs.FirstOrDefault(u => u.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            var jobDto = new JobUpdateDTO
            {
                Description = job.Description,
                Location = job.Location,
                AudioDescription = job.AudioDescription,
                Payout = job.Payout,
                Negotiable = job.Negotiable
            };

            // Apply patch document to jobDto
            patchDto.ApplyTo(jobDto, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Update the job entity with the patched data
            job.Description = jobDto.Description;
            job.Location = jobDto.Location;
            job.AudioDescription = jobDto.AudioDescription;
            job.Payout = jobDto.Payout;
            job.Negotiable = jobDto.Negotiable;

            _db.SaveChanges();

            return Ok(job);
        }


        [Authorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteJob(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }

            var job = _db.Jobs.FirstOrDefault(u => u.Id == id);

            if (job == null)
            {
                return NotFound();
            }


            var category = _db.Categories.FirstOrDefault(c => c.Id == job.CategoryId);
            if (category != null)
            {
                // Decrement the category's NumberOfInstances
                category.NumberOfInstances = Math.Max(0, category.NumberOfInstances - 1);
            }

            _db.Jobs.Remove(job);
            _db.SaveChanges();

            return NoContent();
        }


        [Authorize]
        [HttpPut("users/location")]
        public async Task<IActionResult> UpdateUserLocation([FromBody] LocationDTO locationDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Unauthorized();

            // Update the user location in the database
            await _userService.UpdateUserLocationAsync(userId, locationDto.Latitude, locationDto.Longitude);

            return Ok();
        }

        //[Authorize]
        //[HttpGet("users/locations")]
        //public async Task<IActionResult> GetOtherUsersLocations()
        //{
        //    var locations = await _userService.GetOtherUsersLocationsAsync();
        //    return Ok(locations);
        //}


        private bool HasValidSubscription(ApplicationUser user)
        {
            if (user == null)
                return false;

            if (user.IsSubscriptionActive)
                return true;

            if (user.TrialEndDate > DateTime.UtcNow)
                return true;

            return false;
        }



        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            var jobs = await _db.Jobs
                .Where(job => job.Description.Contains(query) || job.Location.Contains(query))
                .Select(job => new JobDTO
                {
                    Id = job.Id,
                    CategoryId = job.CategoryId,
                    Description = job.Description,
                    Location = job.Location,
                    Status = job.Status,
                    DatePosted = job.DatePosted,
                    Payout = job.Payout,
                    Negotiable = job.Negotiable,
                    UserName = job.UserName,
                    NumberOfTasksCompleted = job.NumberOfTasksCompleted,
                    NumberOfTasksEmployed = job.NumberOfTasksEmployed,
                    UserLastTaskDoneDate = job.UserLastTaskDoneDate,
                    UserLastTaskEmployedDate = job.UserLastTaskEmployedDate,
                    UserRating = job.UserRating
                })
                .ToListAsync();

            return Ok(jobs);
        }
    }
}
