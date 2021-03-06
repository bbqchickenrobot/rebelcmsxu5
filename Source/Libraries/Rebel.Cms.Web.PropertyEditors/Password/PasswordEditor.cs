﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rebel.Cms.Web.Model.BackOffice.PropertyEditors;

namespace Rebel.Cms.Web.PropertyEditors.Password
{
   
    [PropertyEditor(CorePluginConstants.PasswordPropertyEditorId, "Password", "Password")]
    public class PasswordEditor : PropertyEditor<PasswordEditorModel, PasswordPreValueModel>
    {
        public override PasswordEditorModel CreateEditorModel(PasswordPreValueModel preValues)
        {
            return new PasswordEditorModel(preValues);
        }

        public override PasswordPreValueModel CreatePreValueEditorModel()
        {
            return new PasswordPreValueModel();
        }
    }
}
