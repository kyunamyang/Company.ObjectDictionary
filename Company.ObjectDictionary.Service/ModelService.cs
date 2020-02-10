﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Transactions;
using AutoMapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Company.ObjectDictionary.Common;
using Company.ObjectDictionary.Entity;
using Company.ObjectDictionary.ViewModel;
using Company.ObjectDictionary.Service.Interface;
using Company.ObjectDictionary.Repository.Interface;

namespace Company.ObjectDictionary.Service
{
    public class ModelService : ServiceBase, IGenericService<ModelViewModel>, ICodeService<ModelViewModel>
    {
        private readonly IMapper mapper;
        private readonly IGenericCommandRepository<Model> modelCommandRepository;
        private readonly IGenericQueryRepository<Model> modelQueryRepository;
        private readonly IGenericQueryRepository<Field> fieldQueryRepository;
        private readonly IGenericQueryRepository<User> userQueryRepository;

        public ModelService(IMapper mapper,
            IGenericCommandRepository<Model> modelCommandRepository,
            IGenericQueryRepository<Model> modelQueryRepository,
            IGenericQueryRepository<Field> fieldQueryRepository,
            IGenericQueryRepository<User> userQueryRepository)
        {
            this.mapper = mapper;
            this.modelCommandRepository = modelCommandRepository;
            this.modelQueryRepository = modelQueryRepository;
            this.fieldQueryRepository = fieldQueryRepository;
            this.userQueryRepository = userQueryRepository;
        }

        // 자식 개체까지 조회하여 리턴
        public ModelViewModel GetById(Guid id)
        {
            var model = modelQueryRepository.GetById(id);

            var conditions = new ConcurrentDictionary<string, string>();
            conditions.TryAdd("ModelId", id.ToString());

            var fields = fieldQueryRepository.GetAll(conditions);
            var user = userQueryRepository.GetById(new Guid(model.UserId));

            var modelViewModel = mapper.Map<ModelViewModel>(model);
            var fieldViewModels = mapper.Map<IEnumerable<FieldViewModel>>(fields);
            var userViewModel = mapper.Map<UserViewModel>(user);

            modelViewModel.Fields = fieldViewModels.ToList();
            modelViewModel.User = userViewModel;

            return modelViewModel;
        }

        public IEnumerable<ModelViewModel> GetAll(IDictionary<string, string> conditions)
        {
            var models = modelQueryRepository.GetAll(conditions);
            return mapper.Map<IEnumerable<ModelViewModel>>(models);
        }

        public void Create(ModelViewModel modelViewModel)
        {
            var model = mapper.Map<Model>(modelViewModel);
            //using(TransactionScope scope = new TransactionScope())
            //{
            //    modelCommandRepository.Create(model);
            //    scope.Complete();
            //}
            modelCommandRepository.Create(model);
        }

        public void Update(ModelViewModel modelViewModel)
        {
            var model = mapper.Map<Model>(modelViewModel);
            modelCommandRepository.Update(model);
        }

        public void Delete(Guid id)
        {
            modelCommandRepository.Delete(id);
        }

        public string GetClassDefinition(ModelViewModel m)
        {
            var classDeclaration = SyntaxFactory.ClassDeclaration(m.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var propertyDeclarations = new List<MemberDeclarationSyntax>();

            foreach (var p in m.Fields)
            {
                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(p.Type), p.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

                propertyDeclarations.Add(propertyDeclaration);
            }

            classDeclaration = classDeclaration.AddMembers(propertyDeclarations.ToArray());

            return classDeclaration.NormalizeWhitespace()
                .ToFullString();

        }
    }
}
