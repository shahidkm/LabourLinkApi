﻿using AdminService.Dtos.SkillDtos;
using AdminService.Helpers.Common;
using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers
{
    [Route("api/Skill")]
    [ApiController]
    public class SkillController : ControllerBase
    {

        private readonly ISkillService _skillService;    
        public SkillController(ISkillService skillService) 
        {
            _skillService= skillService;
            
        }

        [HttpGet("getAllSkill")]
        [Authorize]
        public async Task<IActionResult> GettAllSkills()
        {

            var response = await _skillService.GetAllSkillAsync();

            if (response.StatusCode == 200)
                return Ok(response); 

            return NotFound(response);


        }

        [HttpGet("getSkillById/{id}")]
        [Authorize]
        public async Task <IActionResult> GetSkillById(Guid id)
        {
            var response=await _skillService.GetSkillByIdAsync(id);

            if(response.StatusCode == 400)
            {
                return BadRequest(response);

            }

            if (response.StatusCode == 200)
            {
                return Ok(response);

            }

            return StatusCode(500, response);   

        }

        [HttpPost("createSkill")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> CreateSkill([FromBody] AddSkillDto newSkill)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<AddSkillDto>(400,"invalid Data"));
            }

             var response= await _skillService.CreateSkillAsync(newSkill);     

            if(response.StatusCode == 201)
            {
                return StatusCode(201, response);   
            }
            return StatusCode(500, response);
        }

        [HttpPut("updateSkill")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSkill([FromBody]  SkillViewDto updateSkill)
        {
            var response=await _skillService.UpdateAsync(updateSkill);

            if(response.StatusCode == 200)
            {
                return Ok(response);
            }
            if(response.StatusCode == 404)
            {
                return NotFound(response);
            }

            return StatusCode(500, response);
        }

        [HttpDelete("deleteSkill/{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteSkill(Guid id)
        {
            var response = await _skillService.DeleteAsync(id);

            if (response.StatusCode == 200)
                return Ok(response); 

            if (response.StatusCode == 400)
                return BadRequest(response);  

            return StatusCode(500, response);  
        }
    }
}
