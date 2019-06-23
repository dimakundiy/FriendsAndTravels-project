﻿using AutoMapper.QueryableExtensions;
using FriendsAndTravel.BAL.Interfaces;
using FriendsAndTravel.Data;
using FriendsAndTravel.Data.CustomDataStructures;
using FriendsAndTravel.Data.Entities;
using FriendsAndTravel.Data.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FriendsAndTravel.BAL.Services
{
    public class PostService : IPostService
    {
        private readonly FriendsAndTravelDbContext db;
        private readonly IPhotoService photoService;
     
        public PostService(FriendsAndTravelDbContext db, IPhotoService photoService)
        {
            this.db = db;
            this.photoService = photoService;
          
        }

        public void Create(string userId, Feeling feeling, string text, IFormFile photo)
        {
            var post = new Post
            {
                UserId = userId,
                Feeling = feeling,
                Text = text,
                Likes = 0,
                Date = DateTime.UtcNow,
                Photo = photo != null ? this.photoService.PhotoAsBytes(photo) : null
            };

            db.Posts.Add(post);
            db.SaveChanges();
        }

        public void Delete(int postId)
        {
            var post = this.db.Posts.Find(postId);
            this.db.Remove(post);
            this.db.SaveChanges();
        }

        public void Edit(int postId, Feeling feeling, string text, IFormFile photo)
        {
            var post = this.db.Posts.Find(postId);
            post.Feeling = feeling;
            post.Text = text;
            post.Photo = photo != null ? this.photoService.PhotoAsBytes(photo) : null;
            this.db.SaveChanges();
        }

        public bool Exists(int id) => this.db.Posts.Any(p => p.Id == id);

        public PaginatedList<PostModel> FriendPostsByUserId(string userId, int pageIndex, int pageSize)
        {
            var friendListIds = this.FriendsIds(userId);

            var posts = this.db
                .Posts
                .Where(p => friendListIds.Contains(p.UserId) || p.UserId == userId)
                .Include(p => p.Comments)
                .ThenInclude(p => p.User)
                .ProjectTo<PostModel>()
                .OrderByDescending(p => p.Date);

            return posts != null ? PaginatedList<PostModel>.Create(posts, pageIndex, pageSize) : null;
        }

        public void Like(int postId)
        {
            if (this.Exists(postId))
            {
                var post = this.db.Posts.Find(postId);
                post.Likes++;
                this.db.SaveChanges();
            }
        }

        public PostModel PostById(int postId)
        {
            return this.db.Posts.Where(p => p.Id == postId).ProjectTo<PostModel>().FirstOrDefault();
        }

        public PaginatedList<PostModel> PostsByUserId(string userId, int pageIndex, int pageSize)
        {
            var posts = this.db
                .Posts
               .Where(p => p.UserId == userId)
                .Include(p => p.Comments)
                .ThenInclude(p => p.User)
                .ProjectTo<PostModel>()
                .OrderByDescending(p => p.Date);

            return posts != null ? PaginatedList<PostModel>.Create(posts.AsNoTracking(), pageIndex, pageSize) : null;
        }

        public bool UserIsAuthorizedToEdit(int postId, string userId) => this.db.Posts.Any(p => p.Id == postId && p.UserId == userId);

        private List<string> FriendsIds(string userId)
        {
            var friends = this.db
                .UserFriend
                .Where(u => u.UserId == userId)
                .Select(u => u.Friend.Id)
                .ToList();

            var otherFriends = this.db
                .UserFriend
                .Where(u => u.FriendId == userId)
                .Select(u => u.User.Id)
                .ToList();

            friends.AddRange(otherFriends);

            return friends;
        }
    }
}
