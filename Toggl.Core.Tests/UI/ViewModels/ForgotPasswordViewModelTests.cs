﻿using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.TestExtensions;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels;
using Toggl.Networking.Exceptions;
using Toggl.Networking.Network;
using Toggl.Shared;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class ForgotPasswordViewModelTests
    {
        public abstract class ForgotPasswordViewModelTest : BaseViewModelTests<ForgotPasswordViewModel, EmailParameter, EmailParameter>
        {
            protected Email ValidEmail { get; } = Email.From("person@company.com");
            protected Email InvalidEmail { get; } = Email.From("This is not an email");

            protected override ForgotPasswordViewModel CreateViewModel()
                => new ForgotPasswordViewModel(TimeService, UserAccessManager, AnalyticsService, NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : ForgotPasswordViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useUserAccessManager,
                bool useAnalyticsService,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var timeService = useTimeService ? TimeService : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ForgotPasswordViewModel(
                        timeService, userAccessManager, analyticsSerivce, navigationService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : ForgotPasswordViewModelTest
        {
            [Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);

                ViewModel.Initialize(EmailParameter.With(email));

                ViewModel.Email.Value.Should().Be(email);
            }
        }

        public sealed class TheResetCommand : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsErrorMessageToEmpty()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.ErrorMessage.Subscribe(observer);

                ViewModel.Reset.Execute();

                observer.LastEmittedValue().Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public void SetsPasswordResetSuccessfulToFalse()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.PasswordResetSuccessful.Subscribe(observer);

                ViewModel.Reset.Execute();

                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ResetsThePassword()
            {
                ViewModel.Email.OnNext(ValidEmail);

                ViewModel.Reset.Execute();

                UserAccessManager.Received().ResetPassword(ValidEmail);
            }

            [Fact, LogIfTooSlow]
            public void TracksPasswordReset()
            {
                ViewModel.Email.OnNext(ValidEmail);

                ViewModel.Reset.Execute();

                AnalyticsService.Received().ResetPassword.Track();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteWhenEmailIsNotValid()
            {
                ViewModel.Email.OnNext(InvalidEmail);

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Reset.Enabled.Subscribe(observer);

                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteWhenEmailIsEmpty()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Reset.Enabled.Subscribe(observer);

                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteIfIsLoading()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.Reset.Execute();

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Reset.Enabled.Subscribe(observer);

                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void SetsErrorMessageToEmptyString()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Throw<string>(new Exception()));
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.ErrorMessage.Subscribe(observer);
                ViewModel.Reset.Execute();
                TestScheduler.Start();

                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.Reset.Execute();
                TestScheduler.Start();

                observer.Messages.Should().HaveCount(3);
                observer.LastEmittedValue().Should().BeEmpty();
            }

            public sealed class WhenPasswordResetSucceeds : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.Reset.Executing.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.LastEmittedValue().Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void SetsPasswordResetSuccessfulToTrue()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.PasswordResetSuccessful.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.LastEmittedValue().Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public void CallsTimeServiceToCloseViewModelAfterFourSeconds()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    TimeService.Received().RunAfterDelay(TimeSpan.FromSeconds(4), Arg.Any<Action>());
                }

                [Fact, LogIfTooSlow]
                public async Task ClosesTheViewModelAfterFourSecondDelay()
                {
                    var testScheduler = new TestScheduler();
                    var timeService = new TimeService(testScheduler);
                    var viewModel = new ForgotPasswordViewModel(
                        timeService, UserAccessManager, AnalyticsService, NavigationService, RxActionFactory);
                    viewModel.AttachView(View);
                    viewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    viewModel.Reset.Execute();
                    TestScheduler.Start();
                    testScheduler.AdvanceBy(TimeSpan.FromSeconds(4).Ticks);

                    var result = await viewModel.Result;
                    result.Email.Should().BeEquivalentTo(ValidEmail);
                }
            }

            public sealed class WhenPasswordResetFails : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.PasswordResetSuccessful.Subscribe(observer);

                    ViewModel.Reset.Execute();

                    observer.LastEmittedValue().Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void SetsNoEmailErrorWhenReceivesBadRequestException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    var exception = new BadRequestException(
                        Substitute.For<IRequest>(), Substitute.For<IResponse>());
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.LastEmittedValue().Should().Be(Resources.PasswordResetEmailDoesNotExistError);
                }

                [Fact, LogIfTooSlow]
                public void SetsOfflineErrorWhenReceivesOfflineException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new OfflineException()));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.LastEmittedValue().Should().Be(Resources.PasswordResetOfflineError);
                }

                [Property]
                public void SetsApiErrorWhenReceivesApiException(NonEmptyString errorString)
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    var exception = new ApiException(
                        Substitute.For<IRequest>(),
                        Substitute.For<IResponse>(),
                        errorString.Get);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.LastEmittedValue().Should().Be(exception.LocalizedApiErrorMessage);
                }

                [Fact, LogIfTooSlow]
                public void SetsGeneralErrorForAnyOtherException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.LastEmittedValue().Should().Be(Resources.PasswordResetGeneralError);
                }
            }
        }

        public sealed class TheCloseWithDefaultResultMethod : ForgotPasswordViewModelTest
        {
            [Property]
            public void ClosesTheViewModelReturningTheEmail(NonEmptyString emailString)
            {
                var viewModel = CreateViewModel();
                viewModel.AttachView(View);
                var email = Email.From(emailString.Get);
                viewModel.Email.OnNext(email);

                viewModel.CloseWithDefaultResult();

                TestScheduler.Start();

                var result = viewModel.Result.GetAwaiter().GetResult();
                result.Email.Should().BeEquivalentTo(email);
            }
        }
    }
}
