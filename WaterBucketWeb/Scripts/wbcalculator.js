(function ($, ko) {
    window.wbc = {};

    wbc.WorkOrder = function (wo) {
        var _self = this;
        var _fields = $.extend({
            InProcess: 'web',
            UseWorker: {}, // need placeholder since the include for the mapping plugin is not working in toJS call to retrieve UseWorker value
            ObserverOnZmq: false,
            YieldWeb: false,
            YieldWorker: false,
            StartDelay: 0,
            WorkDelay: 0
        }, wo);
        ko.mapping.fromJS(_fields, { 'include': ['UseWorker'] }, _self);

        _self.UseWorker = ko.computed({
            read: function () { return _self.InProcess() === 'worker'; },
            write: function (val) { val ? _self.InProcess('worker') : _self.InProcess('web'); }
        });

        // Validation Rules
        _self.StartDelay.extend({ number: true });
        _self.WorkDelay.extend({ number: true });
    };

    wbc.ProblemVM = function (first, second, goal) {
        var _self = this;
        var problemo;
        if (arguments.length > 0) {
            if (arguments.length === 1)
                problemo = first
            else
                problemo = { FirstBucketCapacity: first, SecondBucketCapacity: second, GoalWaterVolume: goal || 0 }
        }
        var _fields = $.extend({
            FirstBucketCapacity: 0,
            SecondBucketCapacity: 0,
            GoalWaterVolume: 0
        }, problemo);
        ko.mapping.fromJS(_fields, {}, _self);

        // Validation Rules
        _self.FirstBucketCapacity.extend({ number: true });
        _self.SecondBucketCapacity.extend({ number: true });
        _self.GoalWaterVolume.extend({ number: true });
    };

    wbc.CalculatorVM = function (options) {
        var _self = this;
        var _hub = options.hub;
        var _fields = $.extend({
            Problem: new wbc.ProblemVM(3, 5, 4),
            Work: new wbc.WorkOrder(),
            BigToSmall: [],
            SmallToBig: [],
            IsStarted: false,
            IsSolvable: true,
            Messages: [],
            UseBoundActions: true
        }, options.data);
        ko.mapping.fromJS(_fields, {}, _self);

        var _bigToSmallUnboundTBody = $('#BigToSmall tbody.unbound');//.get(0);
        var _smallToBigUnboundTBody = $('#SmallToBig tbody.unbound');//.get(0);

        _self.SubmitProblem = function () {
            _self.IsStarted(false);
            _hub.server.solve(//{ FirstBucketCapacity: _self.FirstBucketCapacity(), SecondBucketCapacity: _self.SecondBucketCapacity(), GoalWaterVolume: _self.GoalWaterVolume() },
                ko.mapping.toJS(_self.Problem),
                ko.mapping.toJS(_self.Work));
            //{ YieldWeb: _self.YieldWeb(), UseWorker: _self.UseWorker(), YieldWorker: _self.UseWorker() && _self.YieldWorker(), ObserverOnZmq: _self.UseWorker() && _self.ObserverOnZmq(), StartDelay: _self.StartDelay(), WorkDelay: _self.WorkDelay() });
        };

        _self.ClearMessages = function () {
            _self.Messages([]);
        };

        _self.Submitted = function () {
            _self.ClearMessages();
            _self.UseBoundActions(true);
            _self.BigToSmall([]);
            _self.SmallToBig([]);
            _self.IsSolvable(true);
            _self.BigToSmallSolution(null);
            _self.SmallToBigSolution(null);
            _bigToSmallUnboundTBody.html('');
            _smallToBigUnboundTBody.html('');
            //for (var i in _self.Css.b2s)
            //    i('');
            //for (var j in _self.Css.s2b)
            //    j('');
        };

        _self.Started = function (useBoundActions) {
            _self.IsStarted(true);
            _self.UseBoundActions(useBoundActions);
            _self.AddSoloMessage('Started the Problem');
        };

        _self.AddMessage = function (msg) {
            _self.Messages.push(msg);
        };

        _self.AddSoloMessage = function (msg) {
            _self.ClearMessages();
            _self.AddMessage(msg);
        };

        _self.BigToSmallSolution = ko.observable();
        _self.SmallToBigSolution = ko.observable();
        _self.Css = {
            b2s: { Actions: ko.observable(''), Used: ko.observable(''), ThrownOut: ko.observable('') },
            s2b: { Actions: ko.observable(''), Used: ko.observable(''), ThrownOut: ko.observable('') }
        };

        _self.AssignSolution = function (strategy, soln) {
            switch (strategy) {
                case 'BigToSmall': _self.BigToSmallSolution(soln);
                    break;
                case 'SmallToBig': _self.SmallToBigSolution(soln);
                    break;
            }
            SetSolutionCss();
        };

        function SetSolutionCss() {
            var b2s = _self.BigToSmallSolution(), s2b = _self.SmallToBigSolution();
            if (b2s && s2b) {
                if (b2s.NumberOfActions < s2b.NumberOfActions) {
                    _self.Css.b2s.Actions('winner');
                    _self.Css.s2b.Actions('loser');
                }
                else if (b2s.NumberOfActions > s2b.NumberOfActions) {
                    _self.Css.b2s.Actions('loser');
                    _self.Css.s2b.Actions('winner');
                }
                else {
                    _self.Css.b2s.Actions('tie');
                    _self.Css.s2b.Actions('tie');
                }

                if (b2s.EndingReservoireState.VolumeUsed < s2b.EndingReservoireState.VolumeUsed) {
                    _self.Css.b2s.Used('winner');
                    _self.Css.s2b.Used('loser');
                }
                else if (b2s.EndingReservoireState.VolumeUsed > s2b.EndingReservoireState.VolumeUsed) {
                    _self.Css.b2s.Used('loser');
                    _self.Css.s2b.Used('winner');
                }
                else {
                    _self.Css.b2s.Used('tie');
                    _self.Css.s2b.Used('tie');
                }

                if (b2s.EndingReservoireState.VolumeThrownOut < s2b.EndingReservoireState.VolumeThrownOut) {
                    _self.Css.b2s.ThrownOut('winner');
                    _self.Css.s2b.ThrownOut('loser');
                }
                else if (b2s.EndingReservoireState.VolumeThrownOut > s2b.EndingReservoireState.VolumeThrownOut) {
                    _self.Css.b2s.ThrownOut('loser');
                    _self.Css.s2b.ThrownOut('winner');
                }
                else {
                    _self.Css.b2s.ThrownOut('tie');
                    _self.Css.s2b.ThrownOut('tie');
                }
            }
            else {
                _self.Css.b2s.Actions('');
                _self.Css.s2b.Actions('');
                _self.Css.b2s.Used('');
                _self.Css.s2b.Used('');
                _self.Css.b2s.ThrownOut('');
                _self.Css.s2b.ThrownOut('');
            }
        };

        _self.State = function (b, step) {
            var bstate = _.find(step.EndingState.BucketState, function (i) {
                return i.Name == b;
            });
            return bstate;
        };

        _self.AddActionStep = function (step) {
            if (_self.UseBoundActions()) {
                switch (step.StrategyName) {
                    case 'BigToSmall': _self.BigToSmall.push(step);
                        break;
                    case 'SmallToBig': _self.SmallToBig.push(step);
                        break;
                }
            }
            else {
                switch (step.StrategyName) {
                    case 'BigToSmall': AddActionToView(_bigToSmallUnboundTBody, step);
                        break;
                    case 'SmallToBig': AddActionToView(_smallToBigUnboundTBody, step);
                        break;
                }
            }
        };

        function AddActionToView($tBody, step) {
            $tBody.append('<tr><td class="stepno">' + step.StepNumber + '</td><td class="state">' + _self.State('A', step).CurrentFill + '</td><td class="state">' + _self.State('B', step).CurrentFill + '</td><td>' + step.Description + '</td></tr>');
        }
    };

})(jQuery, ko);