using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ComponentModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using ReSharper.Scout.DebugSymbols;

namespace ReSharper.Scout.Providers
{
	[NavigationProvider(ProgramConfigurations.VS_ADDIN)]
	internal class ReferenceSourceNavigationProvider : INavigationProvider
	{
		#region INavigationProvider members

		public IEnumerable<INavigationPoint> CreateNavigationPoints(object target, IEnumerable<INavigationPoint> basePoints)
		{
			ICompiledElement element = (ICompiledElement) target;
			Shell.Instance.Locks.AssertReadAccessAllowed();
			Logger.Assert(element.IsValid(), "Target should be valid");
			element.GetManager().AssertAllDocumentAreCommited();

			if (!Options.UsePdbFiles || string.IsNullOrEmpty(ReSharper.GetDocId(element))
				|| element.Module == null || element.Module.Name == null)
			{
				return basePoints;
			}

			List<INavigationPoint> points = new ReferenceSource(element).GetNavigationPoints();
			return points == null || points.Count == 0? basePoints: points;
		}

		public IEnumerable<Type> GetSupportedTargetTypes()
		{
			return new Type[] { typeof(ICompiledElement) };
		}

		public double Priority
		{
			get { return 2000.0; }
		}

		#endregion
	}
}