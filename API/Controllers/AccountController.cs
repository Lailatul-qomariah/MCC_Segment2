﻿using API.Contracts;
using API.Data;
using API.DTOs.Accounts;
using API.Models;
using API.Utilities.Handlers;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Transactions;

namespace API.Controllers;
[ApiController]
[Route("api/[controller]")]

public class AccountController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmailHandler _emailHandler;
    private readonly IEducationRepository _educationRepository;
    private readonly IUniversityRepository _universityRepository;




    //contructor dan dependency injection untuk menyimpan instance dari IAccountRepository
    public AccountController(IAccountRepository accountRepository, IEmployeeRepository employeeRepository,
        IEmailHandler emailHandler, IEducationRepository educationRepository, IUniversityRepository universityRepository)
    {
        _accountRepository = accountRepository;
        _employeeRepository = employeeRepository;
        _emailHandler = emailHandler;
        _educationRepository = educationRepository;
        _universityRepository = universityRepository;
    }

    [HttpGet] //menangani request get all data endpoint /Account
    public IActionResult GetAll()
    {
        // Mendapatkan semua data Account dari _accountRepository.
        var result = _accountRepository.GetAll();
        if (!result.Any()) //cek apakah data ditemukan
        {
            //respons dengan kode status HTTP 404(Not Found) dengan pesan kesalahan yang dihasilkan.
            return NotFound(new ResponseErrorHandler
            {
                Code = StatusCodes.Status404NotFound,
                Status = HttpStatusCode.NotFound.ToString(),
                Message = "Data Not Found"
            });
        }
        // mengubah data Account ke dalam format DTO secara explicit.
        var data = result.Select(x => (AccountDto)x);
        //return HTTP OK dan data dalam format DTO dengan kode status 200 untuk success
        return Ok(new ResponseOKHandler<IEnumerable<AccountDto>>(data));
    }

    [HttpGet("{guid}")] //menangani request get data by guid endpoint /Account/{guid}
    public IActionResult GetByGuid(Guid guid)
    {
        //get data berdasarkan guid yang diinputkan user
        var result = _accountRepository.GetByGuid(guid);
        // cek apakah data result kosong atau apakah data berdasarkan guid ditemukan
        if (result is null)
        {
            //respons dengan kode status HTTP 404(Not Found) dengan pesan kesalahan yang dihasilkan.
            return NotFound(new ResponseErrorHandler
            {
                Code = StatusCodes.Status404NotFound,
                Status = HttpStatusCode.NotFound.ToString(),
                Message = "Data Not Found"
            });
        }
        //return HTTP OK dan data dalam format DTO dengan kode status 200 untuk success
        return Ok(new ResponseOKHandler<AccountDto>((AccountDto)result));
    }

    [HttpPost] //menangani request create data ke endpoint /Account
    //parameter berupa objek menggunakan format DTO agar crete data disesuaikan dengan format DTO
    public IActionResult Create(CreateAccountDto accountDto)
    {
        try
        {
            Account toCreate = accountDto;
            toCreate.Password = HashHandler.HashPassword(accountDto.Password);
            // create data account menggunakan format data DTO implisit
            var result = _accountRepository.Create(toCreate);

            //return HTTP OK dan data dalam format DTO dengan kode status 200 untuk success
            return Ok(new ResponseOKHandler<AccountDto>((AccountDto)result));
        }
        catch (Exception ex)
        {
            // return dengan kode status 500 dan menampilkan pesan exception 
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseErrorHandler
            {
                Code = StatusCodes.Status500InternalServerError,
                Status = HttpStatusCode.InternalServerError.ToString(),
                Message = "Failed to create data",
                Error = ex.Message
            });
        }


    }

    [HttpPut] //menangani request update ke endpoint /Account
    //parameter berupa objek menggunakan format DTO explicit agar crete data disesuaikan dengan format DTO
    public IActionResult Update(AccountDto accountDto)
    {
        try
        {
            //get data by guid dan menggunakan format DTO 
            var entity = _accountRepository.GetByGuid(accountDto.Guid);
            if (entity is null) //cek apakah data berdasarkan guid tersedia 
            {
                //respons dengan kode status HTTP 404(Not Found) dengan pesan kesalahan yang dihasilkan.
                return NotFound(new ResponseErrorHandler
                {
                    Code = StatusCodes.Status404NotFound,
                    Status = HttpStatusCode.NotFound.ToString(),
                    Message = "Data Not Found"
                });
            }
            //convert data DTO dari inputan user menjadi objek Account
            Account toUpdate = accountDto;
            //menyimpan createdate yg lama 
            toUpdate.CreatedDate = entity.CreatedDate;
            toUpdate.Password = HashHandler.HashPassword(accountDto.Password);

            //update Account dalam repository
            _accountRepository.Update(toUpdate);

            // return HTTP OK dengan kode status 200 dan return "data updated" untuk sukses update.
            return Ok(new ResponseOKHandler<string>("Data Updated"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseErrorHandler
            {
                Code = StatusCodes.Status500InternalServerError,
                Status = HttpStatusCode.InternalServerError.ToString(),
                Message = "Failed to Update data",
                Error = ex.Message
            });
        }
    }

    [HttpDelete("{guid}")] //menangani request delete ke endpoint /Account
    public IActionResult Delete(Guid guid)
    {

        try
        {
            // get data account by guid
            var entity = _accountRepository.GetByGuid(guid);
            // cek apakah entity (get data by guid) kosong
            if (entity is null)
            {
                //respons dengan kode status HTTP 404(Not Found) dengan pesan kesalahan yang dihasilkan.
                return NotFound(new ResponseErrorHandler
                {
                    Code = StatusCodes.Status404NotFound,
                    Status = HttpStatusCode.NotFound.ToString(),
                    Message = "Data Not Found"
                });
            }
            //delete Account dari repository
            _accountRepository.Delete(entity);

            // return HTTP OK dengan kode status 200 dan return "data updated" untuk sukses update.
            return Ok(new ResponseOKHandler<string>("Data Deleted"));

        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseErrorHandler
            {
                Code = StatusCodes.Status500InternalServerError,
                Status = HttpStatusCode.InternalServerError.ToString(),
                Message = "Failed to Delete data",
                Error = ex.Message
            });
        }

    }

    [HttpPut("ForgotPassWord{email}")]
    public IActionResult ForgotPasword(string emailForgot)
    {

        var employee = _employeeRepository.GetAll();//get all Employee
        var account = _accountRepository.GetByEmail(emailForgot); // get account berdasarkan email

        if (!(employee.Any() && account != null))//cek apakah email dan account by id memiliki data 
        {
            //respons dengan kode status HTTP 404(Not Found) dengan pesan kesalahan yang dihasilkan.
            return NotFound(new ResponseErrorHandler
            {
                Code = StatusCodes.Status404NotFound,
                Status = HttpStatusCode.NotFound.ToString(),
                Message = "Data Not Found"
            });
        }

        int otp = GenerateHandler.GenerateOtp(); //generate otp dengan angka random 
        account.Otp = otp; //set acoount dengan otp yg sudah di generate
        account.IsUsed = false; //set otp belum digunakan
        account.ExpiredTime = DateTime.Now.AddMinutes(5);
        _accountRepository.Update(account);

        var accAll = _accountRepository.GetAll();//get all account
        //layanan mail service yang akan dikirim ketika forgot password
        _emailHandler.Send("Forgot Password", $"Your OTP is {account.Otp}", emailForgot);
        //join employee dan account untuk ditampilkan datanya 
        var accountView = from emp in employee
                          join acc in accAll on emp.Guid equals acc.Guid
                          select new AccountForgotPaswordDto
                          {
                              Otp = acc.Otp,
                              ExpireTime = acc.ExpiredTime
                          };

        // return HTTP OK dengan kode status 200 untuk sukses update dan return data dalam format DTO
        return Ok(new ResponseOKHandler<IEnumerable<AccountForgotPaswordDto>>(accountView));
    }



    [HttpPut("ChangePassword")]
    public IActionResult ChangePassword(AccountChangePasswordDto changePsswd)
    {
        // get account  berdasarkan alamat email
        var account = _accountRepository.GetByEmail(changePsswd.Email);

        if (account == null)
        {
            //respons dengan kode status HTTP 404(Not Found) dengan pesan kesalahan yang dihasilkan.
            return NotFound(new ResponseErrorHandler
            {
                Code = StatusCodes.Status404NotFound,
                Status = HttpStatusCode.NotFound.ToString(),
                Message = "Account Not Found"
            });
        }

        // cek apakah OTP yang diinput sesuai dengan yang ada dalam account
        if (changePsswd.Otp != account.Otp)
        {
            return BadRequest(new ResponseErrorHandler
            {
                Code = StatusCodes.Status400BadRequest,
                Status = HttpStatusCode.BadRequest.ToString(),
                Message = "Invalid OTP"
            });
        }

        // cek apakah OTP sudah digunakan sebelumnya
        if (account.IsUsed)
        {
            return BadRequest(new ResponseErrorHandler
            {
                Code = StatusCodes.Status400BadRequest,
                Status = HttpStatusCode.BadRequest.ToString(),
                Message = "OTP has already been used"
            });
        }
        var currentTime = DateTime.UtcNow; //get date time saat ini
        var expiredTime = account.ExpiredTime; //get date time dari expired time di Dto

        // cek apakah OTP sudah kedaluwarsa
        if (currentTime > expiredTime)
        {
            return BadRequest(new ResponseErrorHandler
            {
                Code = StatusCodes.Status400BadRequest,
                Status = HttpStatusCode.BadRequest.ToString(),
                Message = "OTP has expired"
            });
        }

        // cek apakah NewPassword sama dengan ConfirmPassword
        if (changePsswd.NewPassword != changePsswd.ConfirmPassword)
        {
            return BadRequest(new ResponseErrorHandler
            {
                Code = StatusCodes.Status400BadRequest,
                Status = HttpStatusCode.BadRequest.ToString(),
                Message = "NewPassword and ConfirmPassword do not match"
            });
        }
        //set properti yang akan diupdate datanya
        int otp = GenerateHandler.GenerateOtp();
        account.Otp = otp;
        account.IsUsed = true;
        account.ExpiredTime = DateTime.Now.AddMinutes(5); //batas expired time adalah 5 menit dari waktu update data
        account.Password = HashHandler.HashPassword(changePsswd.NewPassword);
        _accountRepository.Update(account); //update data 
        return Ok(new ResponseOKHandler<string>("Password changed successfully"));
    }

    [HttpPost("Login")]
    public IActionResult Login(LoginDto loginDto)
    {
        try
        {
            var account = _accountRepository.GetByEmail(loginDto.Email); //get email berdasarkan input
            if (account == null)
            {
                // Respons dengan kode status HTTP 404 (Not Found) jika akun tidak ditemukan.
                return NotFound(new ResponseErrorHandler
                {
                    Code = StatusCodes.Status404NotFound,
                    Status = HttpStatusCode.NotFound.ToString(),
                    Message = "Account Not Found"
                });
            }
            //hashing password dari inputan user dan dicek apakah true atau false
            bool hashingPasswd = HashHandler.VerifyPassword(loginDto.Password, account.Password);

            if (!hashingPasswd) //cek hasil hasing true or false
            {
                // Respons dengan pesan kesalahan jika password salah
                return BadRequest(new ResponseErrorHandler
                {
                    Code = StatusCodes.Status400BadRequest,
                    Status = HttpStatusCode.BadRequest.ToString(),
                    Message = "Account or Password is invalid"
                });
            }
            return Ok(new ResponseOKHandler<string>("Login Berhasil"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseErrorHandler
            {
                Code = StatusCodes.Status500InternalServerError,
                Status = HttpStatusCode.InternalServerError.ToString(),
                Message = "Failed to Delete data",
                Error = ex.Message
            });
        }
    }



    [HttpPost("Register")]
    public IActionResult Register(RegisterAccountDto registrationDto)
    {
        using (var transaction = new TransactionScope()) //mengelola transaction dg using (clear after used)
        {
            try
            {
                Employee toCreateEmp = registrationDto; //convert data DTO dari inputan user menjadi objek Employee
                toCreateEmp.Nik = GenerateHandler.GenerateNik(_employeeRepository.GetLastNik()); //set nik dg generate nik
                var resultEmp = _employeeRepository.Create(toCreateEmp); //create data account menggunakan format data DTO implisit

                //cek apakah nama univ dan code nya sudah ada di DB
                var univFindResult = _universityRepository.GetCodeName(registrationDto.UniversityCode, registrationDto.UniversityName);
                if (univFindResult is null)
                {
                    //jika tidak ada maka membuat data baru
                    univFindResult = _universityRepository.Create(registrationDto);
                } 

                Education toCreateEdu = registrationDto;
                toCreateEdu.Guid = resultEmp.Guid; //set Guid Education dengan Guid yang ada pada employee
                toCreateEdu.UniversityGuid = univFindResult.Guid;
                var resultedu = _educationRepository.Create(toCreateEdu);

                //cek apakah password tidak sama dengan confirm password
                if (registrationDto.Password != registrationDto.ConfirmPassword)
                {
                    return BadRequest(new ResponseErrorHandler
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Status = HttpStatusCode.BadRequest.ToString(),
                        Message = "NewPassword and ConfirmPassword do not match"
                    });
                }

                Account toCreateAcc = registrationDto;
                toCreateAcc.Guid = resultEmp.Guid; //set Guid Account dengan Guid yang ada pada employee
                toCreateAcc.Password = HashHandler.HashPassword(registrationDto.Password);
                _accountRepository.Create(toCreateAcc);


                transaction.Complete(); // Commit transaksi 
                return Ok(new ResponseOKHandler<string>("Registration successfully"));
            }
            catch (Exception ex)
            {
                //bertindak sebagai rollback juga
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseErrorHandler
                {
                    Code = StatusCodes.Status500InternalServerError,
                    Status = HttpStatusCode.InternalServerError.ToString(),
                    Message = "Failed Registration Account",
                    Error = ex.Message
                });
            }
        }
    }


}










